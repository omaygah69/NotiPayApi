using NotiPayApi.Models;
using NotiPayApi.Entities;
using NotiPayApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using NotiPayApi.Data; 
using System.Text.Json;

namespace NotiPayApi.Controllers;

[ApiController]
[Route("api/webhooks/xendit")]
public class XenditWebhookController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly UserDb _db;

    public XenditWebhookController(IConfiguration cfg, UserDb db)
    {
        _cfg = cfg;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Post()
    {
        // verify token header
        var tokenHeader = Request.Headers["x-callback-token"].FirstOrDefault();
        var expected = _cfg["Xendit:WebhookVerificationToken"];
        if (string.IsNullOrEmpty(expected) || tokenHeader != expected)
            return Forbid(); // 403 if token mismatch

        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();
        var doc = JsonDocument.Parse(raw);

        // Xendit typically posts event data with invoice/payment details. Adjust parsing per event shape.
        // Example: payment link paid event might contain "external_id" or the payment link id
        if (doc.RootElement.TryGetProperty("id", out var idProp))
        {
            var xenditId = idProp.GetString();
            // try to find PaymentNotice by XenditPaymentLinkId
            var notice = _db.PaymentNotices.FirstOrDefault(p => p.XenditPaymentLinkId == xenditId);
            if (notice != null)
            {
                // find status — this depends on payload; replace with real event field mapping.
                if (doc.RootElement.TryGetProperty("status", out var s))
                {
                    var status = s.GetString();
                    if (status == "PAID" || status == "PAID_SUCCESS" || status == "COMPLETED")
                    {
                        notice.Status = PaymentStatus.Paid;
                        notice.PaidAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                    }
                    else if (status == "EXPIRED")
                    {
                        notice.Status = PaymentStatus.Expired;
                        await _db.SaveChangesAsync();
                    }
                }
            }
        }

        return Ok();
    }
}
