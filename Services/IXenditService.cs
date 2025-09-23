using NotiPayApi.Entities;
using NotiPayApi.Models;
using NotiPayApi.Data;
namespace NotiPayApi.Services;

public interface IXenditService
{
    Task<(string linkId, string url)> CreatePaymentLinkAsync(
        string externalId, 
        decimal amount, 
        string currency, 
        string? description, 
        string? channelCode = null);
}
