// NotiPayApi/Services/IAuthService.cs
using NotiPayApi.Entities;
using NotiPayApi.Models;

namespace NotiPayApi.Services;

public interface IAuthService
{
    Task<User?> RegisterAsync(UserDto request);
    Task<object> LogInAsync(LoginDto request); 
    Task<List<User>> GetAllUsersAsync();

    // Admin methods - School Year Management
    Task<List<SchoolYearDto>> GetSchoolYearsAsync();
    Task<bool> SetSchoolYearAsync(SchoolYearDto dto);
    Task<SchoolYearDto?> GetCurrentSchoolYearAsync();

    // Admin methods - User Management
    Task<List<UserAdminDto>> GetAdminUsersAsync();
    Task<bool> UpdateUserAsync(Guid id, UpdateUserDto dto);
    
    // Admin methods - Appointment Management
    Task<List<AppointmentDto>> GetAppointmentsAsync(DateTime? startDate = null, DateTime? endDate = null);
    
    // Helper method
    Task<UserEligibilityDto> GetUserEligibilityAsync(Guid userId);
}
