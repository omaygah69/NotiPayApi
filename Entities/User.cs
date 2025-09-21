namespace NotiPayApi.Entities;

public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string HashedPassword { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? RefreshToken { get; set; } 
    public DateTime? RefreshTokenExpiry { get; set; }

    public ICollection<PaymentNotice> PaymentNotices { get; set; } = new List<PaymentNotice>();
}
