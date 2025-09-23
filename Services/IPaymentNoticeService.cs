using NotiPayApi.Models;
using NotiPayApi.Entities;
using NotiPayApi.Services;
using NotiPayApi.Data;
using Microsoft.EntityFrameworkCore;

namespace NotiPayApi.Services;

public interface IPaymentNoticeService
{
    Task<PaymentNotice> CreateAsync(Guid adminId, Guid userId, string title, string description, decimal amount, string currency);
    Task<PaymentNotice?> GetByIdAsync(Guid id);
    Task<IEnumerable<PaymentNotice>> ListForUserAsync(Guid userId);
    Task UpdateStatusAsync(Guid id, PaymentStatus status, DateTime? paidAt = null);
    Task<string> CreateOrUpdateXenditPaymentAsync(Guid noticeId, decimal amount, string currency, string channelCode, string title);
    Task UpdatePaymentLinkAsync(PaymentNotice notice);
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

    public async Task<PaymentNotice> CreateAsync(Guid adminId, Guid userId, string title, string description, decimal amount, string currency)
    {
        var notice = new PaymentNotice
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Amount = amount,
            Currency = currency,
            UserId = userId, 
            CreatedAt = DateTime.UtcNow,
            Status = PaymentStatus.Pending
        };

        // external id used for Xendit to identify this payment (unique)
        var externalId = $"noticen_{notice.Id}";
        var (linkId, url) = await _xendit.CreatePaymentLinkAsync(externalId, amount, currency, description, null);  // Pass null for optional channelCode
        notice.XenditPaymentLinkId = linkId;
        notice.XenditPaymentLinkUrl = url;

        _db.PaymentNotices.Add(notice);
        await _db.SaveChangesAsync();

        return notice;
    }

    public Task<PaymentNotice?> GetByIdAsync(Guid id) => _db.PaymentNotices.FindAsync(id).AsTask();

    public async Task<IEnumerable<PaymentNotice>> ListForUserAsync(Guid userId)
    {
        // Make case-insensitive comparison
	return await _db.PaymentNotices
	    .Where(p => p.UserId == userId)  
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

    public async Task<string> CreateOrUpdateXenditPaymentAsync(Guid noticeId, decimal amount, string currency, string channelCode, string title)
    {
        // First, get the notice to use its ID for external ID consistency
        var notice = await _db.PaymentNotices.FindAsync(noticeId);
        if (notice == null)
            throw new ArgumentException("Payment notice not found");

        // Use the same external ID pattern as CreateAsync for consistency
        var externalId = $"noticen_{noticeId}";
        
        try
        {
            // Create payment link using Xendit with the specified channel
            var (linkId, url) = await _xendit.CreatePaymentLinkAsync(
                externalId,
                amount,
                currency,
                title,
                channelCode // Pass the channel code to Xendit service
            );

            // Update the notice with the new payment details
            notice.XenditPaymentLinkId = linkId;
            notice.XenditPaymentLinkUrl = url;
            
            // Update the XenditPayments table if it exists (optional)
            var existingPayment = await _db.XenditPayments
                .FirstOrDefaultAsync(p => p.NoticeId == noticeId);
            
            if (existingPayment != null)
            {
                // Update existing payment
                existingPayment.ChannelCode = channelCode ?? string.Empty;  // Handle possible null
                existingPayment.UpdatedAt = DateTime.UtcNow;
                _db.XenditPayments.Update(existingPayment);
            }
            else
            {
                // Create new payment record
                var newPayment = new XenditPayment
                {
                    Id = Guid.NewGuid(),
                    NoticeId = noticeId,
                    Amount = amount,
                    Currency = currency,
                    ChannelCode = channelCode ?? string.Empty,  // Handle possible null
                    Title = title,
                    LinkId = linkId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.XenditPayments.Add(newPayment);
            }

            await _db.SaveChangesAsync();

            return url;
        }
        catch (Exception ex)
        {
            // Log the exception in production
            throw new InvalidOperationException($"Failed to create Xendit payment: {ex.Message}", ex);
        }
    }
    
    public async Task UpdatePaymentLinkAsync(PaymentNotice notice)
    {
        if (notice == null)
        {
            throw new ArgumentNullException(nameof(notice));
        }
        
        _db.PaymentNotices.Update(notice);
        await _db.SaveChangesAsync();
    }
}
