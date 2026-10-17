namespace EnjoyEveryday.Application.Services;

public class RazorpaySettings
{
    public const string SectionName = "Razorpay";

    public string KeyId { get; set; } = "rzp_test_enjoyeveryday";
    public string KeySecret { get; set; } = "secret_enjoyeveryday_key";
    public string WebhookSecret { get; set; } = "whsec_enjoyeveryday_webhook";
    public string DefaultCurrency { get; set; } = "INR";
    public bool MockMode { get; set; } = false;
}

public class RazorpayGatewayOrderResult
{
    public string OrderId { get; set; } = string.Empty;
    public string Currency { get; set; } = "INR";
    public long AmountInPaise { get; set; }
    public string Receipt { get; set; } = string.Empty;
    public string Status { get; set; } = "created";
}

public interface IRazorpayGateway
{
    RazorpaySettings Settings { get; }
    Task<RazorpayGatewayOrderResult> CreateOrderAsync(long amountInPaise, string currency, string receipt, Dictionary<string, string>? notes = null, CancellationToken ct = default);
    bool VerifyPaymentSignature(string orderId, string paymentId, string signature);
    bool VerifyWebhookSignature(string rawRequestBody, string signatureHeader);
}
