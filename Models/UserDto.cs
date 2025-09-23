namespace NotiPayApi.Models;

public class UserDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty; // added
}

public class LoginDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SchoolYearDto
{
    public int Year { get; set; }
    public DateTime StartDate { get; set; }
}

public class UpdateUserDto
{
    public int? StartSchoolYear { get; set; }
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class AppointmentDto
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public string ProfessorName { get; set; } = string.Empty;
    public string ProfessorEmail { get; set; } = string.Empty;
    public string? StudentName { get; set; }
    public string? StudentEmail { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTime CreatedAt { get; set; }
}

public class UserAdminDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int? StartSchoolYear { get; set; }
    public bool IsEligible { get; set; }
}

public class UserEligibilityDto
{
    public Guid UserId { get; set; }
    public bool IsEligible { get; set; }
    public int? StartSchoolYear { get; set; }
    public int? CurrentSchoolYear { get; set; }
    public string EligibilityMessage { get; set; } = string.Empty;
}
