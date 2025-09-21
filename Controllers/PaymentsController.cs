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
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown-admin";

        var notice = await _svc.CreateAsync(adminId, dto.UserId, dto.Title, dto.Description, dto.Amount, dto.Currency ?? "PHP");
        return CreatedAtAction(nameof(Get), new { id = notice.Id }, notice);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> Get(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token");

        var n = await _svc.GetByIdAsync(id);
        if (n == null) return NotFound();

        // Security: Ensure user can only access their own notices
        if (n.UserId != userId)
            return Forbid("You can only access your own payment notices");

        return Ok(n);
    }

    // ✅ FIXED: Use your existing ListForUserAsync method
    [HttpGet("my-requests")]
    [Authorize]
    public async Task<IActionResult> GetMyRequests()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token");

        var notices = await _svc.ListForUserAsync(userId);
        return Ok(notices);
    }

    // ✅ NEW: Get payment link for a notice (using your existing service)
    [HttpGet("{id}/pay")]
    [Authorize]
    public async Task<IActionResult> GetPaymentLink(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User ID not found in token");

        var notice = await _svc.GetByIdAsync(id);
        if (notice == null)
            return NotFound("Payment notice not found");

        // Security: Ensure user can only access their own notices
        if (notice.UserId != userId)
            return Forbid("You can only access your own payment notices");

        if (notice.Status == PaymentStatus.Paid)
            return BadRequest("This payment has already been completed");

        // ✅ Use your existing XenditPaymentLinkUrl - no need to recreate
        if (string.IsNullOrEmpty(notice.XenditPaymentLinkUrl))
        {
            // If no link exists, you might want to recreate it
            // For now, just return error
            return BadRequest("Payment link not available. Contact administrator.");
        }

        return Ok(new { 
            notice = notice,
            paymentUrl = notice.XenditPaymentLinkUrl 
        });
    }

    // DTO
    public record CreateNoticeDto(string UserId, string Title, string Description, decimal Amount, string? Currency);
}
