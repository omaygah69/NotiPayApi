using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotiPayApi.Models;
using NotiPayApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NotiPayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("register")]
    public async Task<ActionResult> Register(UserDto request)
    {
        var user = await _authService.RegisterAsync(request);
        if (user is null)
            return BadRequest(new { error = "Username Already Exists" });

        // Return safe DTO without sensitive data
        var response = new
        {
            id = user.Id,
            userName = user.UserName,
            email = user.Email,
            message = "Registration successful. Admin approval required for app access."
        };
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult> LogIn(LoginDto request)
    {
        var result = await _authService.LogInAsync(request);

        if (!((dynamic)result).success)
        {
            var error = ((dynamic)result).error;
            return Unauthorized(new { message = error });
        }

        var tokens = ((dynamic)result).data;
        return Ok(tokens);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-check")]
    public IActionResult CheckAdmin()
    {
        return Ok(new { message = "User is Admin", timestamp = DateTime.UtcNow });
    }

    [Authorize]
    [HttpGet]
    public IActionResult CheckAuthenticated()
    {
        return Ok(new { message = "User is authenticated", timestamp = DateTime.UtcNow });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("members")]
    public async Task<ActionResult<List<UserResponseDto>>> GetMembers()
    {
        var users = await _authService.GetAllUsersAsync();

        var response = users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            UserName = u.UserName,
            Role = u.Role,
	    PhoneNumber = u.PhoneNumber
        }).ToList();

        return Ok(response);
    }

    // Admin endpoints for school year management
    [Authorize(Roles = "Admin")]
    [HttpGet("school-years")]
    public async Task<ActionResult<List<SchoolYearDto>>> GetSchoolYears()
    {
        var schoolYears = await _authService.GetSchoolYearsAsync();
        return Ok(schoolYears);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("school-year")]
    public async Task<ActionResult> SetSchoolYear([FromBody] SchoolYearDto dto)
    {
        var success = await _authService.SetSchoolYearAsync(dto);
        if (!success)
            return BadRequest(new { error = "Invalid school year or start date" });

        return Ok(new
        {
            message = "School year updated successfully",
            year = dto.Year,
            startDate = dto.StartDate
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("current-school-year")]
    public async Task<ActionResult<SchoolYearDto>> GetCurrentSchoolYear()
    {
        var currentSchoolYear = await _authService.GetCurrentSchoolYearAsync();
        if (currentSchoolYear == null)
            return NotFound(new { error = "No school year configured" });

        return Ok(currentSchoolYear);
    }

    // Admin endpoints for user management
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-users")]
    public async Task<ActionResult<List<UserAdminDto>>> GetAdminUsers()
    {
        var users = await _authService.GetAdminUsersAsync();
        return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("user/{id}")]
    public async Task<ActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        var success = await _authService.UpdateUserAsync(id, dto);
        if (!success)
            return NotFound(new { error = "User not found" });

        return Ok(new { message = "User updated successfully" });
    }

    // Admin endpoints for appointments
    [Authorize(Roles = "Admin")]
    [HttpGet("appointments")]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointments(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var appointments = await _authService.GetAppointmentsAsync(startDate, endDate);
        return Ok(appointments);
    }

    // Helper endpoint to check user eligibility (for admin dashboard)
    [Authorize(Roles = "Admin")]
    [HttpGet("user/{id}/eligibility")]
    public async Task<ActionResult<UserEligibilityDto>> CheckUserEligibility(Guid id)
    {
        var eligibility = await _authService.GetUserEligibilityAsync(id);
        return Ok(eligibility);
    }
}
