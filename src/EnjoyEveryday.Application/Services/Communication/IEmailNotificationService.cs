using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services;

public class EmailNotificationResult
{
    public bool Success { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}

public class EmailNotificationLogDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string? CcRecipients { get; set; }
    public string Status { get; set; } = "Sent";
    public DateTime SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Metadata { get; set; }
}

public interface IEmailNotificationService
{
    /// <summary>
    /// Direct transactional email dispatch with audit logging and optional mandatory CC.
    /// </summary>
    Task<EmailNotificationResult> SendEmailAsync(
        string toAddress, 
        string subject, 
        string htmlBody, 
        string notificationType = "General",
        Guid? tenantId = null,
        object? metadata = null,
        string? ccAddress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends an invitation to a new Daycare Organization Administrator with a 1-tap activation token link.
    /// </summary>
    Task<EmailNotificationResult> SendOrgAdminInvitationAsync(
        string recipientEmail, 
        string recipientName, 
        string organizationName, 
        string planName, 
        string activationUrl, 
        string? tempPassword = null, 
        Guid? tenantId = null, 
        CancellationToken ct = default);

    /// <summary>
    /// Sends an account lifecycle status update notification (e.g. Active, Suspended, Reactivated) to an organization administrator.
    /// </summary>
    Task<EmailNotificationResult> SendOrganizationLifecycleUpdateAsync(
        string recipientEmail, 
        string organizationName, 
        string newStatus, 
        string reason, 
        string actionUrl, 
        Guid? tenantId = null, 
        CancellationToken ct = default);

    /// <summary>
    /// Sends a subscription plan or tariff update notice to subscribers.
    /// </summary>
    Task<EmailNotificationResult> SendPlanOrTariffUpdateNoticeAsync(
        string recipientEmail, 
        string organizationName, 
        string planName, 
        string updateTitle, 
        string changesSummaryHtml, 
        string effectiveDateText, 
        string billingPortalUrl, 
        Guid? tenantId = null, 
        CancellationToken ct = default);

    /// <summary>
    /// Sends a platform invoice notice to an organization with amount, due date, and payment instructions.
    /// </summary>
    Task<EmailNotificationResult> SendPlatformInvoiceNoticeAsync(
        string recipientEmail, 
        string organizationName, 
        string invoiceNumber, 
        decimal amount, 
        string currency, 
        DateTime dueDate, 
        string invoiceDescription, 
        string invoicePortalUrl, 
        Guid? tenantId = null, 
        CancellationToken ct = default);

    /// <summary>
    /// Sends an official payment receipt confirmation to an organization administrator upon payment verification.
    /// </summary>
    Task<EmailNotificationResult> SendPaymentReceiptConfirmationAsync(
        string recipientEmail, 
        string organizationName, 
        string paymentReference, 
        decimal amount, 
        string currency, 
        string paymentMethod, 
        string billingPortalUrl, 
        Guid? tenantId = null, 
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves recent transactional email logs for platform administration auditing.
    /// </summary>
    Task<List<EmailNotificationLogDto>> GetRecentEmailLogsAsync(int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// Sends an electronic consent signature request to a parent or guardian with mandatory Org Admin CC.
    /// </summary>
    Task<EmailNotificationResult> SendConsentInvitationAsync(
        string recipientEmail, 
        string recipientName, 
        string childFirstName, 
        string documentTitle, 
        string signingUrl, 
        DateTime? deadline, 
        string? orgAdminCcEmail = null,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a reminder notice for pending consent signatures with mandatory Org Admin CC.
    /// </summary>
    Task<EmailNotificationResult> SendConsentReminderAsync(
        string recipientEmail, 
        string recipientName, 
        string childFirstName, 
        string documentTitle, 
        string signingUrl, 
        DateTime? deadline, 
        string? orgAdminCcEmail = null,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a welcome email to an onboarded parent linking them to the parent portal.
    /// </summary>
    Task<EmailNotificationResult> SendParentWelcomeAsync(
        string recipientEmail, 
        string parentName, 
        string childName, 
        string portalUrl, 
        string? orgAdminCcEmail = null,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends child onboarding stage transition notification to parent with mandatory Org Admin CC.
    /// </summary>
    Task<EmailNotificationResult> SendOnboardingStageNotificationAsync(
        string recipientEmail,
        string parentName,
        string childName,
        string stage,
        string stageDescription,
        string daycareName,
        string? actionRequired,
        string? actionUrl,
        string orgAdminCcEmail,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends consent submission confirmation to parent with mandatory Org Admin CC.
    /// </summary>
    Task<EmailNotificationResult> SendConsentSubmittedConfirmationAsync(
        string recipientEmail,
        string parentName,
        string childName,
        string documentTitle,
        string status,
        string orgAdminCcEmail,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends final consent approval/confirmation notice with mandatory Org Admin CC.
    /// </summary>
    Task<EmailNotificationResult> SendConsentApprovedConfirmationAsync(
        string recipientEmail,
        string parentName,
        string childName,
        string documentTitle,
        string orgAdminCcEmail,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a welcome notification to newly onboarded staff or promoted lead teachers.
    /// </summary>
    Task<EmailNotificationResult> SendEducatorWelcomeAsync(
        string recipientEmail, 
        string educatorName, 
        string role, 
        string branchName, 
        string portalUrl, 
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a tuition billing notice with line-item totals and due dates.
    /// </summary>
    Task<EmailNotificationResult> SendTuitionInvoiceNoticeAsync(
        string recipientEmail, 
        string parentName, 
        string childName, 
        string invoiceNumber, 
        decimal amount, 
        DateTime dueDate, 
        string invoiceUrl, 
        Guid? tenantId = null,
        CancellationToken ct = default);
}

