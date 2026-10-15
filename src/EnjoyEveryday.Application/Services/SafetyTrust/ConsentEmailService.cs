using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class EmailDeliveryResult
{
    public bool Success { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}

public interface IConsentEmailService
{
    Task<EmailDeliveryResult> SendSigningInvitationAsync(
        string recipientEmail,
        string recipientName,
        string childFirstName,
        string documentTitle,
        DateTime? deadline,
        string rawToken,
        string baseUrl,
        CancellationToken ct = default);

    Task<EmailDeliveryResult> SendSigningReminderAsync(
        string recipientEmail,
        string recipientName,
        string childFirstName,
        string documentTitle,
        DateTime? deadline,
        string rawToken,
        string baseUrl,
        CancellationToken ct = default);

    Task<EmailDeliveryResult> SendSigningConfirmationAsync(
        string recipientEmail,
        string recipientName,
        string childFirstName,
        string documentTitle,
        DateTime signedAtUtc,
        CancellationToken ct = default);
}

public class ConsentEmailService : IConsentEmailService
{
    private readonly ILogger<ConsentEmailService> _logger;
    // Track sent email logs in memory for audit & inspectability
    private static readonly ConcurrentBag<EmailDeliveryResult> _deliveryHistory = new();

    public ConsentEmailService(ILogger<ConsentEmailService> logger)
    {
        _logger = logger;
    }

    public static IEnumerable<EmailDeliveryResult> DeliveryHistory => _deliveryHistory;

    public async Task<EmailDeliveryResult> SendSigningInvitationAsync(
        string recipientEmail,
        string recipientName,
        string childFirstName,
        string documentTitle,
        DateTime? deadline,
        string rawToken,
        string baseUrl,
        CancellationToken ct = default)
    {
        // Safe signing link: only token in path, no child name, no tenant id, no sensitive params
        var signingUrl = $"{baseUrl.TrimEnd('/')}/consent/sign/{rawToken}";
        var deadlineText = deadline.HasValue ? deadline.Value.ToString("MMM dd, yyyy") : "within 7 days";

        var messageId = $"msg_inv_{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Sending consent invitation email to recipient {Recipient} for document '{DocumentTitle}'. Secure URL generated.",
            recipientEmail, documentTitle);

        // Simulate delivery through outbox/provider
        await Task.Delay(20, ct);

        var result = new EmailDeliveryResult
        {
            Success = true,
            MessageId = messageId,
            SentAtUtc = DateTime.UtcNow
        };
        _deliveryHistory.Add(result);
        return result;
    }

    public async Task<EmailDeliveryResult> SendSigningReminderAsync(
        string recipientEmail,
        string recipientName,
        string childFirstName,
        string documentTitle,
        DateTime? deadline,
        string rawToken,
        string baseUrl,
        CancellationToken ct = default)
    {
        var signingUrl = $"{baseUrl.TrimEnd('/')}/consent/sign/{rawToken}";
        var messageId = $"msg_rem_{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Sending consent reminder email to recipient {Recipient} for document '{DocumentTitle}'.",
            recipientEmail, documentTitle);

        await Task.Delay(20, ct);

        var result = new EmailDeliveryResult
        {
            Success = true,
            MessageId = messageId,
            SentAtUtc = DateTime.UtcNow
        };
        _deliveryHistory.Add(result);
        return result;
    }

    public async Task<EmailDeliveryResult> SendSigningConfirmationAsync(
        string recipientEmail,
        string recipientName,
        string childFirstName,
        string documentTitle,
        DateTime signedAtUtc,
        CancellationToken ct = default)
    {
        var messageId = $"msg_conf_{Guid.NewGuid():N}";
        _logger.LogInformation(
            "Sending consent confirmation email to recipient {Recipient} for document '{DocumentTitle}'.",
            recipientEmail, documentTitle);

        await Task.Delay(20, ct);

        var result = new EmailDeliveryResult
        {
            Success = true,
            MessageId = messageId,
            SentAtUtc = DateTime.UtcNow
        };
        _deliveryHistory.Add(result);
        return result;
    }
}
