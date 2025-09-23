using NotiPayApi.Models;
using NotiPayApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using NotiPayApi.Entities;

namespace NotiPayApi.Controllers;

[ApiController]
[Route("api/payment-notices")]
public class PaymentNoticeController : ControllerBase
{
    private readonly IPaymentNoticeService _svc;

    public PaymentNoticeController(IPaymentNoticeService svc) => _svc = svc;

    [HttpGet("debug/user")]
    [Authorize]
    public IActionResult DebugUserInfo()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(new
        {
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            AllClaims = claims
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateNoticeDto dto)
    {
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminIdClaim) || !Guid.TryParse(adminIdClaim, out var adminId))
            return Unauthorized("Invalid admin ID in token");

        var notice = await _svc.CreateAsync(adminId, dto.UserId, dto.Title, dto.Description, dto.Amount, dto.Currency ?? "PHP");
        return CreatedAtAction(nameof(Get), new { id = notice.Id }, notice);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> Get(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid user ID in token");

        var n = await _svc.GetByIdAsync(id);
        if (n == null) return NotFound();

        if (n.UserId != userId)
            return Forbid("You can only access your own payment notices");

        return Ok(n);
    }

    [HttpGet("my-requests")]
    [Authorize]
    public async Task<IActionResult> GetMyRequests()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid user ID in token");

        var notices = await _svc.ListForUserAsync(userId);
        return Ok(notices);
    }

    [HttpGet("{id}/pay")]
    [Authorize]
    public async Task<IActionResult> GetPaymentLink(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid user ID in token");

        var notice = await _svc.GetByIdAsync(id);
        if (notice == null)
            return NotFound("Payment notice not found");

        if (notice.UserId != userId)
            return Forbid("You can only access your own payment notices");

        if (notice.Status == PaymentStatus.Paid)
            return BadRequest("This payment has already been completed");

        if (string.IsNullOrEmpty(notice.XenditPaymentLinkUrl))
            return BadRequest("Payment link not available. Contact administrator.");

        return Ok(new
        {
            notice,
            paymentUrl = notice.XenditPaymentLinkUrl
        });
    }

    [HttpPost("{id}/pay")]
    [Authorize]
    public async Task<IActionResult> ProcessPayment(Guid id, [FromBody] ProcessPaymentDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("Invalid user ID in token");

        var notice = await _svc.GetByIdAsync(id);
        if (notice == null)
            return NotFound("Payment notice not found");

        if (notice.UserId != userId)
            return Forbid("You can only pay your own payment notices");

        if (notice.Status == PaymentStatus.Paid)
            return BadRequest("This payment has already been completed");

        try
        {
            var paymentUrl = await _svc.CreateOrUpdateXenditPaymentAsync(
                notice.Id,
                notice.Amount,
                notice.Currency,
                dto.ChannelCode,
                notice.Title
            );

            notice.XenditPaymentLinkUrl = paymentUrl;
            await _svc.UpdatePaymentLinkAsync(notice);

            return Ok(new
            {
                success = true,
                paymentUrl,
                notice
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Payment processing failed", details = ex.Message });
        }
    }

    public record ProcessPaymentDto(string ChannelCode);

    public record CreateNoticeDto(Guid UserId, string Title, string Description, decimal Amount, string? Currency);
}
