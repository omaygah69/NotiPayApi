using NotiPayApi.Models;
using NotiPayApi.Entities;
using NotiPayApi.Services;
using NotiPayApi.Data;
using Microsoft.EntityFrameworkCore; 
namespace NotiPayApi.Services;

public interface IPaymentNoticeService
{
    Task<PaymentNotice> CreateAsync(string adminId, string userId, string title, string description, decimal amount, string currency);
    Task<PaymentNotice?> GetByIdAsync(Guid id);
    Task<IEnumerable<PaymentNotice>> ListForUserAsync(string userId);
    Task UpdateStatusAsync(Guid id, PaymentStatus status, DateTime? paidAt = null);
}

public class PaymentNoticeService : IPaymentNoticeService
{
    private readonly UserDb _db;
    private readonly IXenditService _xendit;

    public PaymentNoticeService(UserDb db, IXenditService xendit)
    {
        _db = db;
        _xendit = xendit;
    }

    public async Task<PaymentNotice> CreateAsync(string adminId, string userId, string title, string description, decimal amount, string currency)
    {
        var notice = new PaymentNotice
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Amount = amount,
            Currency = currency,
	    UserId = userId.ToLower(), 
            CreatedAt = DateTime.UtcNow,
            Status = PaymentStatus.Pending
        };

        // external id used for Xendit to identify this payment (unique)
        var externalId = $"noticen_{notice.Id}";
        var (linkId, url) = await _xendit.CreatePaymentLinkAsync(externalId, amount, currency, description);
        notice.XenditPaymentLinkId = linkId;
        notice.XenditPaymentLinkUrl = url;

        _db.PaymentNotices.Add(notice);
        await _db.SaveChangesAsync();

        return notice;
    }

    public Task<PaymentNotice?> GetByIdAsync(Guid id) => _db.PaymentNotices.FindAsync(id).AsTask();

    public async Task<IEnumerable<PaymentNotice>> ListForUserAsync(string userId)
    {
	//  Make case-insensitive comparison
	return await _db.PaymentNotices
	    .Where(p => p.UserId.ToLower() == userId.ToLower()) 
	    .OrderByDescending(p => p.CreatedAt)
	    .ToListAsync();
    }

    public async Task UpdateStatusAsync(Guid id, PaymentStatus status, DateTime? paidAt = null)
    {
        var p = await _db.PaymentNotices.FindAsync(id);
        if (p is null) return;
        p.Status = status;
        if (paidAt.HasValue) p.PaidAt = paidAt;
        await _db.SaveChangesAsync();
    }
}
