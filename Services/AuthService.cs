using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotiPayApi.Data;
using NotiPayApi.Entities;
using NotiPayApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace NotiPayApi.Services;

public class AuthService(UserDb context, IConfiguration configuration) : IAuthService
{
    public async Task<User?> RegisterAsync(UserDto request)
    {
        // Check if username already exists
        if (await context.Users.AnyAsync(u => u.UserName == request.UserName))
            return null;

        User user = new()
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Role = "User", // Default to User (professor)
            RefreshToken = null,
            RefreshTokenExpiry = null,
            StartSchoolYear = null // New users are ineligible until admin sets this
        };

        var hashedPassword = new PasswordHasher<User>()
            .HashPassword(user, request.Password);
        user.HashedPassword = hashedPassword;

        // Save user
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<object> LogInAsync(LoginDto request)
    {
        // Find user
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
        if (user is null)
            return new { success = false, error = "Invalid username or password" };

        // Verify password
        var verifyHash = new PasswordHasher<User>()
            .VerifyHashedPassword(user, user.HashedPassword, request.Password);
        if (verifyHash == PasswordVerificationResult.Failed)
            return new { success = false, error = "Invalid username or password" };

        // Enforce professor eligibility rule (skip for Admin)
        if (user.Role == "User")
        {
            var currentSchoolYear = await context.SchoolYears
                .Where(s => s.StartDate <= DateTime.UtcNow)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (currentSchoolYear == null)
                return new { 
                    success = false, 
                    error = "School year not configured. Contact administrator." 
                };

            if (user.StartSchoolYear == null)
                return new { 
                    success = false, 
                    error = "Your account is not yet activated for app usage. Contact administrator." 
                };

            int yearsTaught = currentSchoolYear.Year - user.StartSchoolYear.Value;
            if (yearsTaught < 1) // Need at least 1 full year (2 semesters)
            {
                return new { 
                    success = false, 
                    error = $"You are not yet eligible to use the app. You need to complete {1 - yearsTaught} more school year(s) of teaching to access the app." 
                };
            }
        }

        // Success - generate tokens
        var tokens = new TokenResponseDto
        {
            AccessToken = CreateToken(user),
            RefreshToken = await GenerateAndSaveRefreshTokenAsync(user),
            Role = user.Role
        };

        return new { success = true, data = tokens };
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        // Only return necessary fields; don't expose passwords or refresh tokens
        return await context.Users
            .Where(u => u.Role == "User")
            .Select(u => new User // Project to avoid sensitive data
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Role = u.Role
                // Don't include HashedPassword, RefreshToken, etc.
            })
            .ToListAsync();
    }

    // Enhanced version for admin dashboard with eligibility info
    public async Task<List<UserAdminDto>> GetAdminUsersAsync()
    {
        var currentSchoolYear = await context.SchoolYears
            .Where(s => s.StartDate <= DateTime.UtcNow)
            .OrderByDescending(s => s.StartDate)
            .Select(s => s.Year)
            .FirstOrDefaultAsync();

        return await context.Users
            .Where(u => u.Role == "User") // Only professors
            .Select(u => new UserAdminDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                StartSchoolYear = u.StartSchoolYear,
                IsEligible = u.StartSchoolYear.HasValue && currentSchoolYear > 0 && 
                           (currentSchoolYear - u.StartSchoolYear.Value >= 1)
            })
            .ToListAsync();
    }

    // Admin methods - School Year Management
    public async Task<List<SchoolYearDto>> GetSchoolYearsAsync()
    {
        return await context.SchoolYears
            .OrderByDescending(s => s.Year)
            .Select(s => new SchoolYearDto
            {
                Year = s.Year,
                StartDate = s.StartDate
            })
            .ToListAsync();
    }

    public async Task<bool> SetSchoolYearAsync(SchoolYearDto dto)
    {
        if (dto.Year < 2000 || dto.StartDate.Year != dto.Year)
            return false;

        var existing = await context.SchoolYears.FirstOrDefaultAsync(s => s.Year == dto.Year);
        if (existing != null)
        {
            existing.StartDate = dto.StartDate;
        }
        else
        {
            var newSchoolYear = new SchoolYear
            {
                Id = Guid.NewGuid(),
                Year = dto.Year,
                StartDate = dto.StartDate
            };
            context.SchoolYears.Add(newSchoolYear);
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<SchoolYearDto?> GetCurrentSchoolYearAsync()
    {
        return await context.SchoolYears
            .Where(s => s.StartDate <= DateTime.UtcNow)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SchoolYearDto
            {
                Year = s.Year,
                StartDate = s.StartDate
            })
            .FirstOrDefaultAsync();
    }

    // Admin methods - User Management
    public async Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
            return false;

        if (dto.StartSchoolYear.HasValue)
            user.StartSchoolYear = dto.StartSchoolYear.Value;

        if (!string.IsNullOrEmpty(dto.Email))
            user.Email = dto.Email;

        if (!string.IsNullOrEmpty(dto.PhoneNumber))
            user.PhoneNumber = dto.PhoneNumber;

        if (!string.IsNullOrEmpty(dto.Role))
            user.Role = dto.Role;

        await context.SaveChangesAsync();
        return true;
    }

    // Admin methods - Appointment Management
    public async Task<List<AppointmentDto>> GetAppointmentsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = context.Appointments
            .Include(a => a.User)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(a => a.Time >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.Time <= endDate.Value);

        return await query
            .OrderBy(a => a.Time)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                Time = a.Time,
                ProfessorName = a.User.UserName,
                ProfessorEmail = a.User.Email,
                StudentName = a.StudentName,
                StudentEmail = a.StudentEmail,
                Description = a.Description,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    // Helper method to check user eligibility
    public async Task<UserEligibilityDto> GetUserEligibilityAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return new UserEligibilityDto 
            { 
                UserId = userId, 
                IsEligible = false, 
                EligibilityMessage = "User not found" 
            };

        var currentSchoolYear = await context.SchoolYears
            .Where(s => s.StartDate <= DateTime.UtcNow)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        bool isEligible = false;
        string eligibilityMessage = "";

        if (user.Role != "User")
        {
            isEligible = true;
            eligibilityMessage = "Admin users are always eligible";
        }
        else if (currentSchoolYear == null)
        {
            eligibilityMessage = "No school year configured";
        }
        else if (user.StartSchoolYear == null)
        {
            eligibilityMessage = "Start school year not set by administrator";
        }
        else
        {
            int yearsTaught = currentSchoolYear.Year - user.StartSchoolYear.Value;
            isEligible = yearsTaught >= 1;
            eligibilityMessage = yearsTaught >= 1 
                ? $"Eligible - has taught {yearsTaught} year(s)"
                : $"Not eligible - needs {1 - yearsTaught} more year(s) of teaching";
        }

        return new UserEligibilityDto
        {
            UserId = userId,
            IsEligible = isEligible,
            StartSchoolYear = user.StartSchoolYear,
            CurrentSchoolYear = currentSchoolYear?.Year,
            EligibilityMessage = eligibilityMessage
        };
    }

    // Token generation methods
    private string CreateToken(User user)
    {
        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
        };

        SymmetricSecurityKey key = new(
            Encoding.UTF8.GetBytes(configuration.GetValue<string>("AppSettings:Token")!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenDescriptor = new JwtSecurityToken(
            issuer: configuration.GetValue<string>("AppSettings:Issuer"),
            audience: configuration.GetValue<string>("AppSettings:Audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddDays(3),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    private string GenerateRefreshToken()
    {
        byte[] randNum = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randNum);
        return Convert.ToBase64String(randNum);
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
    {
        string refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await context.SaveChangesAsync();
        return refreshToken;
    }
}
