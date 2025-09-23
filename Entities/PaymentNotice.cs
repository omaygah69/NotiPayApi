namespace NotiPayApi.Entities;

public enum PaymentStatus { Pending, Paid, Expired, Failed, Cancelled }

public class PaymentNotice
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PHP";
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string? XenditPaymentLinkId { get; set; }
    public string? XenditPaymentLinkUrl { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}
