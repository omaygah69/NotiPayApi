namespace NotiPayApi.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string UserName { get; set; }
    public string? HashedPassword { get; set; }
    public required string Email { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Role { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public int? StartSchoolYear { get; set; }

    public ICollection<PaymentNotice> PaymentNotices { get; set; } = new List<PaymentNotice>();
}

public class SchoolYear
{
    public Guid Id { get; set; }
    public int Year { get; set; }  
    public DateTime StartDate { get; set; }  
}

public class Appointment
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public Guid ProfessorId { get; set; }  // Changed from UserId to ProfessorId
    public required User User { get; set; }         // Keep User for navigation (EF convention)
    
    // Other fields
    public string? Description { get; set; }
    public string? StudentName { get; set; }
    public string? StudentEmail { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
