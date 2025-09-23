using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NotiPayApi.Services;

public class XenditService : IXenditService
{
    private readonly IHttpClientFactory _http;
    private readonly string _apiKey;

    public XenditService(IHttpClientFactory http, IConfiguration cfg)
    {
        _http = http;
        _apiKey = cfg["Xendit:ApiKey"] ?? throw new ArgumentNullException("Xendit:ApiKey missing");
    }

    // 4-parameter version (for backward compatibility)
    public async Task<(string linkId, string url)> CreatePaymentLinkAsync(
        string externalId, decimal amount, string currency, string? description)
    {
        return await CreatePaymentLinkAsync(externalId, amount, currency, description, null);
    }

    // 5-parameter version (with channelCode support)
    public async Task<(string linkId, string url)> CreatePaymentLinkAsync(
        string externalId, decimal amount, string currency, string? description, string? channelCode = null)
    {
        var client = _http.CreateClient("xendit");

        // Basic Auth
        var authBytes = Encoding.ASCII.GetBytes($"{_apiKey}:");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        // Use the latest API version
        client.DefaultRequestHeaders.Remove("x-api-version");
        client.DefaultRequestHeaders.Add("x-api-version", "2023-10-01");

        // Build payload for Invoice API (simpler and more reliable)
        var payload = new
        {
            external_id = externalId,
            payer_email = "customer@example.com", // Make this dynamic if needed
            description = description ?? "Payment Request",
            amount = (double)amount, // Convert to double for JSON
            currency = currency.ToUpper(), // Must be uppercase (e.g., "IDR")
            
            // Optional: Configure redirect URLs
            success_redirect_url = "https://your-app.com/success",
            failure_redirect_url = "https://your-app.com/failure",
            
            // Don't send email from Xendit - handle it yourself
            should_send_email = false,
            
            // Channel code support (if provided)
            channel_code = channelCode,
            
            // Optional: Configure available payment methods (use ONE of these approaches)
            // Approach 1: Allow all common methods
            allowed_payment_methods = new[] { "CARD", "EWALLET", "BANK_TRANSFER" },
            
            // Approach 2: (uncomment to use instead) Specific payment method
            // payment_method = new { type = "DIRECT_DEBIT" }
        };

        // Use the Invoice API endpoint (more reliable for payment links)
        var response = await client.PostAsJsonAsync("invoices", payload);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Xendit API returned {(int)response.StatusCode} {response.ReasonPhrase}: {errorContent}");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        // Extract invoice ID and URL
        var linkId = doc.RootElement.GetProperty("id").GetString()!;
        var url = doc.RootElement.GetProperty("invoice_url").GetString()!;

        return (linkId, url);
    }
}
