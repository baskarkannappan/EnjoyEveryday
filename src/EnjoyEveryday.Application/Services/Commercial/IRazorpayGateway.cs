namespace EnjoyEveryday.Application.Services;

public class RazorpaySettings
{
    public const string SectionName = "Razorpay";

    public string KeyId { get; set; } = string.Empty;
    public string KeySecret { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = "INR";
    public bool MockMode { get; set; } = true;
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

    Task<RazorpayGatewayOrderResult> CreateOrderWithCredentialsAsync(
        string keyId,
        string keySecret,
        long amountInPaise,
        string currency,
        string receipt,
        Dictionary<string, string>? notes = null,
        CancellationToken ct = default)
        => CreateOrderAsync(amountInPaise, currency, receipt, notes, ct);

    bool VerifyPaymentSignatureWithSecret(string orderId, string paymentId, string signature, string keyId, string keySecret)
        => VerifyPaymentSignature(orderId, paymentId, signature);

    bool VerifyWebhookSignatureWithSecret(string rawRequestBody, string signatureHeader, string webhookSecret)
        => VerifyWebhookSignature(rawRequestBody, signatureHeader);
}
