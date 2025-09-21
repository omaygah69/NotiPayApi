using NotiPayApi.Entities;
using NotiPayApi.Models;
using NotiPayApi.Data;
namespace NotiPayApi.Services;

public interface IXenditService
{
    Task<(string PaymentId, string RedirectUrl)> CreatePaymentLinkAsync(
        string externalId, decimal amount, string currency, string? description);
}
