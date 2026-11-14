using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Dapper;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Services.Communication;

public class AzureCommunicationEmailService : IEmailNotificationService
{
    private readonly IConfiguration _configuration;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AzureCommunicationEmailService> _logger;

    public AzureCommunicationEmailService(
        IConfiguration configuration,
        IDbConnectionFactory connectionFactory,
        ITenantContext tenantContext,
        ILogger<AzureCommunicationEmailService> logger)
    {
        _configuration = configuration;
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<EmailNotificationResult> SendEmailAsync(
        string toAddress,
        string subject,
        string htmlBody,
        string notificationType = "General",
        Guid? tenantId = null,
        object? metadata = null,
        string? ccAddress = null,
        CancellationToken ct = default)
    {
        var effectiveTenantId = tenantId ?? _tenantContext.TenantId;
        if (effectiveTenantId == Guid.Empty)
        {
            effectiveTenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        }

        var connectionString = _configuration["AzureCommunicationServices:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("AzureCommunicationServices__ConnectionString")
                ?? Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_SERVICES_CONNECTION_STRING");
        }

        var senderAddress = _configuration["AzureCommunicationServices:SenderAddress"];
        if (string.IsNullOrWhiteSpace(senderAddress))
        {
            senderAddress = Environment.GetEnvironmentVariable("AzureCommunicationServices__SenderAddress")
                ?? Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_SERVICES_SENDER_ADDRESS")
                ?? "DoNotReply@e86634f3-653b-48cc-a7f6-37cc501c0604.azurecomm.net";
        }

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(senderAddress))
        {
            _logger.LogWarning("[ACS Email] ConnectionString or SenderAddress is not configured. Email to {To} (CC: {Cc}) skipped.", toAddress, ccAddress ?? "None");
            await LogAuditAsync(effectiveTenantId, notificationType, toAddress, ccAddress, "Skipped", "ACS credentials not configured", metadata, ct);

            return new EmailNotificationResult
            {
                Success = false,
                ErrorMessage = "Azure Communication Services is not configured.",
                MessageId = $"skipped_{Guid.NewGuid():N}"
            };
        }

        try
        {
            var emailClient = new EmailClient(connectionString);
            var emailContent = new EmailContent(subject)
            {
                Html = htmlBody
            };

            var emailMessage = new EmailMessage(
                senderAddress: senderAddress,
                recipientAddress: toAddress,
                content: emailContent);

            if (!string.IsNullOrWhiteSpace(ccAddress))
            {
                emailMessage.Recipients.CC.Add(new EmailAddress(ccAddress.Trim()));
            }

            var globalAdminCc = _configuration["AzureCommunicationServices:AdminCcAddress"];
            if (!string.IsNullOrWhiteSpace(globalAdminCc) && !string.Equals(globalAdminCc.Trim(), ccAddress?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                emailMessage.Recipients.CC.Add(new EmailAddress(globalAdminCc.Trim()));
            }

            var operation = await emailClient.SendAsync(WaitUntil.Started, emailMessage, ct);
            var messageId = operation.Id ?? $"msg_{Guid.NewGuid():N}";

            _logger.LogInformation("[ACS Email] Dispatched {Type} email to {To} (CC: {Cc}). MessageId: {MessageId}", notificationType, toAddress, ccAddress ?? "None", messageId);

            await LogAuditAsync(effectiveTenantId, notificationType, toAddress, ccAddress, "Sent", null, metadata, ct);

            return new EmailNotificationResult
            {
                Success = true,
                MessageId = messageId,
                SentAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ACS Email] Failed to send {Type} email to {To} (CC: {Cc})", notificationType, toAddress, ccAddress ?? "None");
            await LogAuditAsync(effectiveTenantId, notificationType, toAddress, ccAddress, "Failed", ex.Message, metadata, ct);

            return new EmailNotificationResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                MessageId = $"err_{Guid.NewGuid():N}"
            };
        }
    }

    public async Task<EmailNotificationResult> SendConsentInvitationAsync(
        string recipientEmail,
        string recipientName,
        string childFirstName,
        string documentTitle,
        string signingUrl,
        DateTime? deadline,
        string? orgAdminCcEmail = null,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Consent Signature Required: {documentTitle} for {childFirstName} — EnjoyEveryday";
        var deadlineText = deadline.HasValue ? deadline.Value.ToString("dd MMMM yyyy") : "within 7 days";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "SAFETY & LEGAL CONSENT",
            headerTitle: "Digital Consent Signature Required",
            greeting: $"Dear {recipientName},",
            introParagraph: $"Your daycare center has requested your review and electronic signature on the following policy document for <strong>{childFirstName}</strong>.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Document:</strong> {documentTitle}</div>
                <div style='margin-bottom: 8px;'><strong>Child:</strong> {childFirstName}</div>
                <div><strong>Submission Deadline:</strong> <span style='color: #8c3b2d; font-weight: 700;'>{deadlineText}</span></div>",
            ctaButtonText: "Review & Sign Document",
            ctaUrl: signingUrl,
            footerNote: "This document is processed securely in accordance with early childhood privacy compliance and statutory regulations."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "ConsentInvitation", tenantId, new { childFirstName, documentTitle, deadlineText }, orgAdminCcEmail, ct);
    }

    public async Task<EmailNotificationResult> SendConsentReminderAsync(
        string recipientEmail,
        string recipientName,
        string childFirstName,
        string documentTitle,
        string signingUrl,
        DateTime? deadline,
        string? orgAdminCcEmail = null,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Reminder: Please Sign {documentTitle} for {childFirstName} — EnjoyEveryday";
        var deadlineText = deadline.HasValue ? deadline.Value.ToString("dd MMMM yyyy") : "as soon as possible";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "REMINDER · ACTION REQUIRED",
            headerTitle: "Pending Consent Signature",
            greeting: $"Dear {recipientName},",
            introParagraph: $"This is a friendly reminder that the document <strong>{documentTitle}</strong> for <strong>{childFirstName}</strong> is awaiting your digital signature.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Document:</strong> {documentTitle}</div>
                <div style='margin-bottom: 8px;'><strong>Child:</strong> {childFirstName}</div>
                <div><strong>Requested By:</strong> {deadlineText}</div>",
            ctaButtonText: "Sign Document Now",
            ctaUrl: signingUrl,
            footerNote: "Please complete this at your earliest convenience to ensure all permissions and emergency protocols remain current."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "ConsentReminder", tenantId, new { childFirstName, documentTitle }, orgAdminCcEmail, ct);
    }

    public async Task<EmailNotificationResult> SendParentWelcomeAsync(
        string recipientEmail,
        string parentName,
        string childName,
        string portalUrl,
        string? orgAdminCcEmail = null,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Welcome to EnjoyEveryday — {childName}'s Daycare Journey";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "FAMILY ONBOARDING",
            headerTitle: $"Welcome to the Family Portal!",
            greeting: $"Dear {parentName},",
            introParagraph: $"We are delighted to welcome <strong>{childName}</strong> to our daycare family! Your parent portal account gives you real-time visibility into your child's daily moments, attendance, care routines, and classroom learning.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Child Enrolled:</strong> {childName}</div>
                <div style='margin-bottom: 8px;'><strong>Features Available:</strong> Daily Care Feed, Photo Memories, Secure Pickup Management, Educator Messaging</div>
                <div><strong>Portal Access:</strong> Authorized via {recipientEmail}</div>",
            ctaButtonText: "Open Parent Portal",
            ctaUrl: portalUrl,
            footerNote: "You can sign in securely anytime using your registered Google account."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "ParentWelcome", tenantId, new { childName }, orgAdminCcEmail, ct);
    }

    public async Task<EmailNotificationResult> SendOnboardingStageNotificationAsync(
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
        CancellationToken ct = default)
    {
        var subject = $"Update: {childName}'s Enrollment Status — {stage} ({daycareName})";

        var detailsContent = $@"
            <div style='margin-bottom: 8px;'><strong>Child:</strong> {childName}</div>
            <div style='margin-bottom: 8px;'><strong>Daycare Center:</strong> {daycareName}</div>
            <div style='margin-bottom: 8px;'><strong>Current Status:</strong> <span style='color: #31523c; font-weight: 700;'>{stage}</span></div>
            <div><strong>Details:</strong> {stageDescription}</div>";

        if (!string.IsNullOrWhiteSpace(actionRequired))
        {
            detailsContent += $@"<div style='margin-top: 10px; padding-top: 8px; border-top: 1px dashed #c8dacd;'><strong>Action Required:</strong> <span style='color: #8c3b2d;'>{actionRequired}</span></div>";
        }

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "ADMISSIONS & ENROLLMENT",
            headerTitle: $"Enrollment Update: {stage}",
            greeting: $"Dear {parentName},",
            introParagraph: $"We are pleased to provide an update regarding <strong>{childName}</strong>'s enrollment at <strong>{daycareName}</strong>.",
            detailsBox: detailsContent,
            ctaButtonText: !string.IsNullOrWhiteSpace(actionUrl) ? "View Application in Portal" : "Open Parent Portal",
            ctaUrl: actionUrl ?? "https://localhost:7005/",
            footerNote: "This status notice is automatically delivered. Organization administration has been CC'd on this notice."
        );

        return await SendEmailAsync(recipientEmail, subject, body, $"Onboarding_{stage}", tenantId, new { childName, stage, daycareName }, orgAdminCcEmail, ct);
    }

    public async Task<EmailNotificationResult> SendConsentSubmittedConfirmationAsync(
        string recipientEmail,
        string parentName,
        string childName,
        string documentTitle,
        string status,
        string orgAdminCcEmail,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Consent Form Submitted: {documentTitle} for {childName} — EnjoyEveryday";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "CONSENT CONFIRMATION",
            headerTitle: "Consent Form Submitted Successfully",
            greeting: $"Dear {parentName},",
            introParagraph: $"Thank you for submitting the required consent agreement for <strong>{childName}</strong>. Your response has been securely recorded in our compliance register.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Document:</strong> {documentTitle}</div>
                <div style='margin-bottom: 8px;'><strong>Child:</strong> {childName}</div>
                <div style='margin-bottom: 8px;'><strong>Recorded Status:</strong> <span style='color: #31523c; font-weight: 700;'>{status}</span></div>
                <div><strong>Submitted At:</strong> {DateTime.UtcNow:dd MMMM yyyy HH:mm} UTC</div>",
            ctaButtonText: "View Consent Records",
            ctaUrl: "https://localhost:7005/family",
            footerNote: "A verified digital copy of this record is maintained for statutory licensing audits. The Organization Admin has been CC'd."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "ConsentSubmitted", tenantId, new { childName, documentTitle, status }, orgAdminCcEmail, ct);
    }

    public async Task<EmailNotificationResult> SendConsentApprovedConfirmationAsync(
        string recipientEmail,
        string parentName,
        string childName,
        string documentTitle,
        string orgAdminCcEmail,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Consent Confirmed & Approved: {documentTitle} for {childName} — EnjoyEveryday";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "CONSENT APPROVAL",
            headerTitle: "Consent Agreement Fully Approved",
            greeting: $"Dear {parentName},",
            introParagraph: $"The daycare administration has reviewed and confirmed final approval for <strong>{documentTitle}</strong> for <strong>{childName}</strong>.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Document:</strong> {documentTitle}</div>
                <div style='margin-bottom: 8px;'><strong>Child:</strong> {childName}</div>
                <div style='margin-bottom: 8px;'><strong>Status:</strong> <span style='color: #31523c; font-weight: 700;'>Active & Approved</span></div>
                <div><strong>Approved Date:</strong> {DateTime.UtcNow:dd MMMM yyyy}</div>",
            ctaButtonText: "Open Parent Portal",
            ctaUrl: "https://localhost:7005/family",
            footerNote: "Your child's safety and care permissions are now active for classroom activities."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "ConsentApproved", tenantId, new { childName, documentTitle }, orgAdminCcEmail, ct);
    }

    public async Task<EmailNotificationResult> SendEducatorWelcomeAsync(
        string recipientEmail,
        string educatorName,
        string role,
        string branchName,
        string portalUrl,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Welcome to EnjoyEveryday Team — {role} at {branchName}";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "TEAM & EDUCATORS",
            headerTitle: $"Welcome to the Team, {educatorName}!",
            greeting: $"Dear {educatorName},",
            introParagraph: $"Your educator account has been established in EnjoyEveryday as <strong>{role}</strong> for the <strong>{branchName}</strong> location.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Assigned Role:</strong> {role}</div>
                <div style='margin-bottom: 8px;'><strong>Branch Location:</strong> {branchName}</div>
                <div><strong>Workspace Login:</strong> {recipientEmail}</div>",
            ctaButtonText: "Access Teacher Workspace",
            ctaUrl: portalUrl,
            footerNote: "Less typing. More teaching. Welcome to a calmer, child-centered educator experience."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "EducatorWelcome", tenantId, new { educatorName, role, branchName }, ccAddress: null, ct: ct);
    }

    public async Task<EmailNotificationResult> SendTuitionInvoiceNoticeAsync(
        string recipientEmail,
        string parentName,
        string childName,
        string invoiceNumber,
        decimal amount,
        DateTime dueDate,
        string invoiceUrl,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Tuition Statement #{invoiceNumber} for {childName} — EnjoyEveryday";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "FINANCIAL STATEMENT",
            headerTitle: "Tuition Invoice Notice",
            greeting: $"Dear {parentName},",
            introParagraph: $"A tuition and care statement has been generated for <strong>{childName}</strong>.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Invoice Number:</strong> #{invoiceNumber}</div>
                <div style='margin-bottom: 8px;'><strong>Total Due:</strong> <span style='font-size: 18px; font-weight: 800; color: #31523c;'>₹{amount:N2}</span></div>
                <div><strong>Due Date:</strong> {dueDate:dd MMMM yyyy}</div>",
            ctaButtonText: "View Invoice Details",
            ctaUrl: invoiceUrl,
            footerNote: "For questions regarding your billing schedule or payment reconciliation, please reply or message the daycare administration."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "TuitionInvoice", tenantId, new { invoiceNumber, amount, dueDate }, ccAddress: null, ct: ct);
    }

    public async Task<EmailNotificationResult> SendOrgAdminInvitationAsync(
        string recipientEmail,
        string recipientName,
        string organizationName,
        string planName,
        string activationUrl,
        string? tempPassword = null,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Welcome to EnjoyEveryday — Activate Your Daycare Account for {organizationName}";

        var passwordDetails = !string.IsNullOrWhiteSpace(tempPassword)
            ? $"<div style='margin-bottom: 8px;'><strong>Initial Password:</strong> <code style='background: #eef2ed; padding: 2px 6px; border-radius: 4px; font-weight: 700;'>{tempPassword}</code></div>"
            : "<div style='margin-bottom: 8px;'><strong>Authentication:</strong> Secure magic link or Google SSO</div>";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "DAYCARE ACCOUNT ACTIVATION",
            headerTitle: $"Welcome to EnjoyEveryday, {organizationName}!",
            greeting: $"Dear {recipientName},",
            introParagraph: $"Your daycare organization, <strong>{organizationName}</strong>, has been successfully provisioned on the EnjoyEveryday platform under the <strong>{planName}</strong> plan.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Organization:</strong> {organizationName}</div>
                <div style='margin-bottom: 8px;'><strong>Subscription Plan:</strong> {planName}</div>
                <div style='margin-bottom: 8px;'><strong>Administrator Email:</strong> {recipientEmail}</div>
                {passwordDetails}
                <div style='font-size: 12px; color: #6c7870; margin-top: 6px;'>Note: This activation link is secure and valid for 7 days.</div>",
            ctaButtonText: "🚀 Activate Account & Sign In",
            ctaUrl: activationUrl,
            footerNote: "Upon signing in, you will be guided to set up your primary branch, classrooms, and educator rosters."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "OrgAdminInvitation", tenantId, new { organizationName, planName, activationUrl }, ccAddress: null, ct: ct);
    }

    public async Task<EmailNotificationResult> SendOrganizationLifecycleUpdateAsync(
        string recipientEmail,
        string organizationName,
        string newStatus,
        string reason,
        string actionUrl,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Organization Account Status Update: {newStatus} — {organizationName}";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "ACCOUNT STATUS NOTIFICATION",
            headerTitle: $"Account Status: {newStatus}",
            greeting: "Dear Daycare Administrator,",
            introParagraph: $"The operational status for <strong>{organizationName}</strong> has been updated to <strong>{newStatus}</strong> by the platform administration.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Organization:</strong> {organizationName}</div>
                <div style='margin-bottom: 8px;'><strong>Current Status:</strong> <span style='font-weight: 700; color: #31523c;'>{newStatus}</span></div>
                <div style='margin-bottom: 8px;'><strong>Reason / Operational Note:</strong> {reason}</div>
                <div><strong>Effective Date:</strong> {DateTime.UtcNow:dd MMMM yyyy}</div>",
            ctaButtonText: "Access Daycare Workspace",
            ctaUrl: actionUrl,
            footerNote: "If you have questions regarding this account status change, please reply to this notice or contact platform support."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "OrgLifecycleUpdate", tenantId, new { organizationName, newStatus, reason }, ccAddress: null, ct: ct);
    }

    public async Task<EmailNotificationResult> SendPlanOrTariffUpdateNoticeAsync(
        string recipientEmail,
        string organizationName,
        string planName,
        string updateTitle,
        string changesSummaryHtml,
        string effectiveDateText,
        string billingPortalUrl,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Subscription & Tariff Update: {planName} — {organizationName}";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "SUBSCRIPTION & COMMERCIAL UPDATE",
            headerTitle: $"Plan Update: {planName}",
            greeting: "Dear Daycare Director,",
            introParagraph: $"We are writing to inform you of an important update to your <strong>{planName}</strong> subscription plan for <strong>{organizationName}</strong>.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Update:</strong> {updateTitle}</div>
                <div style='margin-bottom: 8px;'><strong>Effective Date:</strong> {effectiveDateText}</div>
                <div style='margin-top: 10px; padding-top: 10px; border-top: 1px dashed #e1e6e0;'>
                    {changesSummaryHtml}
                </div>",
            ctaButtonText: "Review Plan & Invoices",
            ctaUrl: billingPortalUrl,
            footerNote: "Your current services and experience modules continue without interruption. You may review your active tier in the billing portal."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "PlanTariffUpdate", tenantId, new { organizationName, planName, updateTitle }, ccAddress: null, ct: ct);
    }

    public async Task<EmailNotificationResult> SendPlatformInvoiceNoticeAsync(
        string recipientEmail,
        string organizationName,
        string invoiceNumber,
        decimal amount,
        string currency,
        DateTime dueDate,
        string invoiceDescription,
        string invoicePortalUrl,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Platform Commercial Invoice #{invoiceNumber} for {organizationName}";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "PLATFORM BILLING STATEMENT",
            headerTitle: $"Invoice #{invoiceNumber}",
            greeting: "Dear Billing Administrator,",
            introParagraph: $"A new commercial invoice has been generated for <strong>{organizationName}</strong>.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Invoice Number:</strong> #{invoiceNumber}</div>
                <div style='margin-bottom: 8px;'><strong>Description:</strong> {invoiceDescription}</div>
                <div style='margin-bottom: 8px;'><strong>Total Due:</strong> <span style='font-size: 18px; font-weight: 800; color: #31523c;'>{currency} {amount:N2}</span></div>
                <div><strong>Due Date:</strong> {dueDate:dd MMMM yyyy}</div>",
            ctaButtonText: "💳 View & Pay Invoice",
            ctaUrl: invoicePortalUrl,
            footerNote: "Payment receipts will be issued automatically upon transaction verification. For payment inquiries, reply directly to this message."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "PlatformInvoice", tenantId, new { invoiceNumber, amount, dueDate, organizationName }, ccAddress: null, ct: ct);
    }

    public async Task<EmailNotificationResult> SendPaymentReceiptConfirmationAsync(
        string recipientEmail,
        string organizationName,
        string paymentReference,
        decimal amount,
        string currency,
        string paymentMethod,
        string billingPortalUrl,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Payment Verified & Receipt — {paymentReference} for {organizationName}";

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "PAYMENT RECEIPT & RECONCILIATION",
            headerTitle: "Payment Verified Successfully",
            greeting: "Dear Daycare Administrator,",
            introParagraph: $"We have verified and recorded your payment of <strong>{currency} {amount:N2}</strong> for <strong>{organizationName}</strong>.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Payment Reference:</strong> {paymentReference}</div>
                <div style='margin-bottom: 8px;'><strong>Amount Credited:</strong> <span style='font-size: 18px; font-weight: 800; color: #31523c;'>{currency} {amount:N2}</span></div>
                <div style='margin-bottom: 8px;'><strong>Payment Method:</strong> {paymentMethod}</div>
                <div><strong>Status:</strong> Reconciled to Organization Ledger</div>",
            ctaButtonText: "View Account Ledger",
            ctaUrl: billingPortalUrl,
            footerNote: "Thank you for partnering with EnjoyEveryday to provide thoughtful, experience-based early childhood education."
        );

        return await SendEmailAsync(recipientEmail, subject, body, "PaymentReceipt", tenantId, new { paymentReference, amount, organizationName }, ccAddress: null, ct: ct);
    }

    public async Task<List<EmailNotificationLogDto>> GetRecentEmailLogsAsync(int limit = 50, CancellationToken ct = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(ct);
            const string sql = @"
                SELECT 
                    id, 
                    tenant_id AS TenantId, 
                    notification_type AS NotificationType, 
                    recipient_email AS RecipientEmail, 
                    cc_recipients AS CcRecipients, 
                    status AS Status, 
                    sent_at AS SentAt, 
                    error_message AS ErrorMessage, 
                    metadata::text AS Metadata
                FROM email_notification_logs
                ORDER BY sent_at DESC
                LIMIT @Limit;";

            var records = await connection.QueryAsync<EmailNotificationLogDto>(sql, new { Limit = limit });
            return records.AsList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent email notification logs");
            return new List<EmailNotificationLogDto>();
        }
    }

    private async Task LogAuditAsync(
        Guid tenantId,
        string notificationType,
        string recipientEmail,
        string? ccRecipients,
        string status,
        string? errorMessage,
        object? metadata,
        CancellationToken ct)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(ct);
            const string sql = @"
                INSERT INTO email_notification_logs 
                    (tenant_id, notification_type, recipient_email, cc_recipients, status, error_message, metadata)
                VALUES 
                    (@TenantId, @NotificationType, @RecipientEmail, @CcRecipients, @Status, @ErrorMessage, @Metadata::jsonb);";

            var json = metadata != null ? JsonSerializer.Serialize(metadata) : null;

            await connection.ExecuteAsync(sql, new
            {
                TenantId = tenantId,
                NotificationType = notificationType,
                RecipientEmail = recipientEmail,
                CcRecipients = ccRecipients,
                Status = status,
                ErrorMessage = errorMessage,
                Metadata = json
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ACS Email] Failed to record audit log for email to {To}", recipientEmail);
        }
    }

    private static string BuildBrandedEmailTemplate(
        string headerEyebrow,
        string headerTitle,
        string greeting,
        string introParagraph,
        string detailsBox,
        string ctaButtonText,
        string ctaUrl,
        string footerNote)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <meta name='viewport' content='width=device-width,initial-scale=1'>
</head>
<body style='margin:0;padding:0;background:#f5f5ef;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",Roboto,Helvetica,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f5ef;padding:32px 16px;'>
    <tr>
      <td align='center'>
        <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border:1px solid #e1e6e0;border-radius:16px;overflow:hidden;max-width:600px;width:100%;box-shadow:0 8px 30px rgba(40,55,47,0.06);'>
          
          <!-- BRAND HEADER -->
          <tr>
            <td style='background:#31523c;padding:32px 36px;text-align:left;'>
              <div style='color:#e6eee9;font-size:11px;font-weight:800;letter-spacing:1.5px;text-transform:uppercase;'>ENJOYEVERYDAY DAYCARE</div>
              <h1 style='margin:8px 0 0;color:#ffffff;font-size:22px;font-weight:750;letter-spacing:-0.3px;'>{headerTitle}</h1>
            </td>
          </tr>

          <!-- CONTENT BODY -->
          <tr>
            <td style='padding:36px;'>
              <div style='font-size:11px;font-weight:800;color:#557c6c;letter-spacing:1px;text-transform:uppercase;margin-bottom:8px;'>{headerEyebrow}</div>
              <p style='margin:0 0 16px;color:#26332d;font-size:16px;font-weight:600;'>{greeting}</p>
              <p style='margin:0 0 24px;color:#4a5c51;font-size:14.5px;line-height:1.6;'>{introParagraph}</p>

              <!-- DETAILS CONTAINER -->
              <div style='background:#f8f9f5;border:1px solid #e4e7df;border-radius:12px;padding:20px 24px;margin-bottom:28px;color:#26332d;font-size:13.5px;line-height:1.5;'>
                {detailsBox}
              </div>

              <!-- ACTION BUTTON -->
              <div style='text-align:center;margin:32px 0 24px;'>
                <a href='{ctaUrl}' style='display:inline-block;background:#31523c;color:#ffffff;text-decoration:none;font-size:14px;font-weight:700;padding:14px 32px;border-radius:9px;letter-spacing:0.2px;'>
                  {ctaButtonText} &rarr;
                </a>
              </div>

              <p style='margin:0;color:#6c7870;font-size:12.5px;line-height:1.5;text-align:center;'>
                {footerNote}
              </p>
            </td>
          </tr>

          <!-- FOOTER -->
          <tr>
            <td style='background:#f8f9f5;border-top:1px solid #e4e7df;padding:20px 36px;text-align:center;'>
              <p style='margin:0;color:#7c877f;font-size:11.5px;'>&copy; {DateTime.UtcNow.Year} EnjoyEveryday. Child-centered early education management.</p>
              <p style='margin:4px 0 0;color:#9aa59d;font-size:11px;'>This is a secure system notification intended solely for the designated recipient.</p>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}

