namespace NotiPayApi.Entities; 

public class XenditPayment
{
    public Guid Id { get; set; }
    public Guid NoticeId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? ChannelCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string LinkId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property (optional)
    public PaymentNotice? Notice { get; set; }
}
