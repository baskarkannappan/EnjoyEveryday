using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnjoyEveryday.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnjoyEveryday.Infrastructure.Services;

public class RazorpayGateway : IRazorpayGateway
{
    private readonly HttpClient _httpClient;
    private readonly RazorpaySettings _settings;
    private readonly ILogger<RazorpayGateway> _logger;

    public RazorpaySettings Settings => _settings;

    public RazorpayGateway(
        HttpClient httpClient,
        IOptions<RazorpaySettings> options,
        ILogger<RazorpayGateway> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://api.razorpay.com/v1/");
        }
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<RazorpayGatewayOrderResult> CreateOrderAsync(
        long amountInPaise, 
        string currency, 
        string receipt, 
        Dictionary<string, string>? notes = null, 
        CancellationToken ct = default)
    {
        if (amountInPaise <= 0)
        {
            throw new ArgumentException("Amount in paise must be positive.", nameof(amountInPaise));
        }

        // Mock / Offline Test Mode check
        if (_settings.MockMode || 
            string.IsNullOrWhiteSpace(_settings.KeySecret) || 
            _settings.KeySecret.StartsWith("secret_enjoyeveryday_key", StringComparison.OrdinalIgnoreCase) ||
            _settings.KeySecret.Contains("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Creating Razorpay order in offline test mode for receipt {Receipt}, amount {Amount}", receipt, amountInPaise);
            return new RazorpayGatewayOrderResult
            {
                OrderId = "order_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "_" + Guid.NewGuid().ToString("N")[..8],
                AmountInPaise = amountInPaise,
                Currency = currency.ToUpperInvariant(),
                Receipt = receipt,
                Status = "created"
            };
        }

        var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.KeyId}:{_settings.KeySecret}"));
        using var request = new HttpRequestMessage(HttpMethod.Post, "orders");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);

        var payload = new
        {
            amount = amountInPaise,
            currency = currency.ToUpperInvariant(),
            receipt,
            notes = notes ?? new Dictionary<string, string>()
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Razorpay order creation failed with status {StatusCode}: {Response}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Razorpay order creation failed (status {(int)response.StatusCode}). Check server logs.");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        return new RazorpayGatewayOrderResult
        {
            OrderId = root.GetProperty("id").GetString() ?? string.Empty,
            AmountInPaise = root.GetProperty("amount").GetInt64(),
            Currency = root.GetProperty("currency").GetString() ?? currency,
            Receipt = root.TryGetProperty("receipt", out var r) ? r.GetString() ?? receipt : receipt,
            Status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "created" : "created"
        };
    }

    public bool VerifyPaymentSignature(string orderId, string paymentId, string signature)
    {
        if (string.IsNullOrWhiteSpace(orderId) || 
            string.IsNullOrWhiteSpace(paymentId) || 
            string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        try
        {
            // Offline mock test verification fallback: if running in mock mode and using signature "mock_sig_valid"
            if (_settings.MockMode && signature == "mock_sig_valid")
            {
                return true;
            }

            var secretBytes = Encoding.UTF8.GetBytes(_settings.KeySecret);
            var payloadBytes = Encoding.UTF8.GetBytes($"{orderId.Trim()}|{paymentId.Trim()}");

            using var hmac = new HMACSHA256(secretBytes);
            var computedHash = hmac.ComputeHash(payloadBytes);
            var expectedHex = Convert.ToHexStringLower(computedHash);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedHex),
                Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant())
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while verifying Razorpay payment signature");
            return false;
        }
    }

    public bool VerifyWebhookSignature(string rawRequestBody, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(rawRequestBody) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        try
        {
            if (_settings.MockMode && signatureHeader == "mock_webhook_valid")
            {
                return true;
            }

            var secret = !string.IsNullOrWhiteSpace(_settings.WebhookSecret) ? _settings.WebhookSecret : _settings.KeySecret;
            var secretBytes = Encoding.UTF8.GetBytes(secret);
            var bodyBytes = Encoding.UTF8.GetBytes(rawRequestBody);

            using var hmac = new HMACSHA256(secretBytes);
            var computedHash = hmac.ComputeHash(bodyBytes);
            var expectedHex = Convert.ToHexStringLower(computedHash);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedHex),
                Encoding.UTF8.GetBytes(signatureHeader.Trim().ToLowerInvariant())
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while verifying Razorpay webhook signature");
            return false;
        }
    }
}
