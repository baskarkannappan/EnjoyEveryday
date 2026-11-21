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

public partial class AzureCommunicationEmailService
{
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

        // Generate instant payment QR code data URI
        var qrPayload = _qrCodeService.BuildInvoicePaymentPayload(invoiceNumber, amount, currency, Guid.Empty, invoicePortalUrl);
        var qrDataUri = _qrCodeService.GeneratePngDataUri(qrPayload, moduleSize: 5, border: 2);

        var body = BuildBrandedEmailTemplate(
            headerEyebrow: "PLATFORM BILLING STATEMENT",
            headerTitle: $"Invoice #{invoiceNumber}",
            greeting: "Dear Billing Administrator,",
            introParagraph: $"A new commercial invoice has been generated for <strong>{organizationName}</strong>.",
            detailsBox: $@"
                <div style='margin-bottom: 8px;'><strong>Invoice Number:</strong> #{invoiceNumber}</div>
                <div style='margin-bottom: 8px;'><strong>Description:</strong> {invoiceDescription}</div>
                <div style='margin-bottom: 8px;'><strong>Total Due:</strong> <span style='font-size: 18px; font-weight: 800; color: #31523c;'>{currency} {amount:N2}</span></div>
                <div style='margin-bottom: 14px;'><strong>Due Date:</strong> {dueDate:dd MMMM yyyy}</div>
                
                <!-- Instant Mobile Payment QR Code Block -->
                <div style='margin-top: 14px; padding: 16px; background: #ffffff; border: 1px dashed #52745e; border-radius: 12px; text-align: center;'>
                    <div style='font-size: 11px; font-weight: 750; text-transform: uppercase; letter-spacing: 0.5px; color: #52745e; margin-bottom: 8px;'>📱 Scan with Phone to Pay Instantly</div>
                    <img src='{qrDataUri}' width='150' height='150' alt='Scan to Pay QR Code' style='display: block; margin: 0 auto 8px; border-radius: 8px; border: 1px solid #e4e7df;' />
                    <div style='font-size: 11.5px; color: #6c7870;'>Scan with your phone camera or banking app to pay {currency} {amount:N2} securely.</div>
                </div>",
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

    public async Task<EmailNotificationResult> SendPlanChangeRequestedAsync(
        string platformAdminEmail,
        string organizationName,
        string orgAdminEmail,
        string currentPlanName,
        string requestedPlanName,
        string requestType,
        string? notes,
        string adminPortalUrl,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Plan {requestType} Request — {organizationName} ({currentPlanName} → {requestedPlanName})";
        var safeNotes = string.IsNullOrWhiteSpace(notes) ? "No additional notes provided." : System.Net.WebUtility.HtmlEncode(notes);
        var detailsBox = $@"
            <table width='100%' cellpadding='4' cellspacing='0' style='font-size:13.5px;'>
                <tr><td style='color:#6c7870;width:160px;'>Organization</td><td style='font-weight:700;'>{System.Net.WebUtility.HtmlEncode(organizationName)}</td></tr>
                <tr><td style='color:#6c7870;'>Requested By</td><td>{System.Net.WebUtility.HtmlEncode(orgAdminEmail)}</td></tr>
                <tr><td style='color:#6c7870;'>Request Type</td><td style='font-weight:700;color:#31523c;'>{System.Net.WebUtility.HtmlEncode(requestType)}</td></tr>
                <tr><td style='color:#6c7870;'>Current Plan</td><td>{System.Net.WebUtility.HtmlEncode(currentPlanName)}</td></tr>
                <tr><td style='color:#6c7870;'>Requested Plan</td><td style='font-weight:700;'>{System.Net.WebUtility.HtmlEncode(requestedPlanName)}</td></tr>
                <tr><td style='color:#6c7870;vertical-align:top;'>Org Admin Notes</td><td>{safeNotes}</td></tr>
            </table>";

        var html = BuildBrandedEmailTemplate(
            headerTitle: $"Subscription {requestType} Request",
            headerEyebrow: "PLATFORM COMMERCIAL WORKFLOW",
            greeting: "Hello Platform Administrator,",
            introParagraph: $"{System.Net.WebUtility.HtmlEncode(organizationName)} has submitted a request to {requestType.ToLowerInvariant()} their subscription plan. Review the request, configure the proposed tariff/rate, and send it back to the Organization Admin for acceptance.",
            detailsBox: detailsBox,
            ctaButtonText: "Review Plan Change Request",
            ctaUrl: adminPortalUrl,
            footerNote: "A copy of this notification has been logged in Platform Email Audit Logs.");

        return await SendEmailAsync(
            toAddress: platformAdminEmail,
            subject: subject,
            htmlBody: html,
            notificationType: "PlanChangeRequested",
            tenantId: tenantId,
            metadata: new { organizationName, orgAdminEmail, currentPlanName, requestedPlanName, requestType },
            ccAddress: orgAdminEmail,
            ct: ct);
    }

    public async Task<EmailNotificationResult> SendPlanChangeProposalForAcceptanceAsync(
        string orgAdminEmail,
        string organizationName,
        string currentPlanName,
        string proposedPlanName,
        string proposedTariffName,
        string formattedRate,
        string effectiveDateText,
        string? platformNotes,
        string subscriptionPortalUrl,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var subject = $"Action Required: Accept Updated Subscription Plan for {organizationName}";
        var safeNotes = string.IsNullOrWhiteSpace(platformNotes) ? "Standard plan terms apply." : System.Net.WebUtility.HtmlEncode(platformNotes);
        var detailsBox = $@"
            <table width='100%' cellpadding='4' cellspacing='0' style='font-size:13.5px;'>
                <tr><td style='color:#6c7870;width:160px;'>Organization</td><td style='font-weight:700;'>{System.Net.WebUtility.HtmlEncode(organizationName)}</td></tr>
                <tr><td style='color:#6c7870;'>Current Plan</td><td>{System.Net.WebUtility.HtmlEncode(currentPlanName)}</td></tr>
                <tr><td style='color:#6c7870;'>Proposed Plan</td><td style='font-weight:700;color:#31523c;'>{System.Net.WebUtility.HtmlEncode(proposedPlanName)}</td></tr>
                <tr><td style='color:#6c7870;'>Billing Schedule</td><td>{System.Net.WebUtility.HtmlEncode(proposedTariffName)}</td></tr>
                <tr><td style='color:#6c7870;'>Rate</td><td style='font-weight:700;'>{System.Net.WebUtility.HtmlEncode(formattedRate)}</td></tr>
                <tr><td style='color:#6c7870;'>Effective Date</td><td>{System.Net.WebUtility.HtmlEncode(effectiveDateText)}</td></tr>
                <tr><td style='color:#6c7870;vertical-align:top;'>Platform Notes</td><td>{safeNotes}</td></tr>
            </table>";

        var html = BuildBrandedEmailTemplate(
            headerTitle: "Subscription Plan Proposal Ready",
            headerEyebrow: "ORGANIZATION SUBSCRIPTION",
            greeting: $"Hello {System.Net.WebUtility.HtmlEncode(organizationName)} Team,",
            introParagraph: "Our platform administration team has prepared your updated subscription plan details. Please review the new plan limits and pricing below, and click the button to accept and activate your subscription.",
            detailsBox: detailsBox,
            ctaButtonText: "Review & Accept Plan",
            ctaUrl: subscriptionPortalUrl,
            footerNote: "Your current plan remains active until you review and accept this proposal.");

        return await SendEmailAsync(
            toAddress: orgAdminEmail,
            subject: subject,
            htmlBody: html,
            notificationType: "PlanChangeProposalForAcceptance",
            tenantId: tenantId,
            metadata: new { organizationName, currentPlanName, proposedPlanName, proposedTariffName, formattedRate, effectiveDateText },
            ct: ct);
    }

    public async Task<EmailNotificationResult> SendPlanChangeDecisionConfirmationAsync(
        string orgAdminEmail,
        string platformAdminEmail,
        string organizationName,
        string planName,
        bool accepted,
        string? decisionNotes,
        string subscriptionPortalUrl,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        var decisionWord = accepted ? "Accepted & Activated" : "Declined";
        var subject = $"Subscription Plan {decisionWord} — {organizationName} ({planName})";
        var safeNotes = string.IsNullOrWhiteSpace(decisionNotes) ? "None" : System.Net.WebUtility.HtmlEncode(decisionNotes);
        var detailsBox = $@"
            <table width='100%' cellpadding='4' cellspacing='0' style='font-size:13.5px;'>
                <tr><td style='color:#6c7870;width:160px;'>Organization</td><td style='font-weight:700;'>{System.Net.WebUtility.HtmlEncode(organizationName)}</td></tr>
                <tr><td style='color:#6c7870;'>Plan</td><td style='font-weight:700;color:#31523c;'>{System.Net.WebUtility.HtmlEncode(planName)}</td></tr>
                <tr><td style='color:#6c7870;'>Decision</td><td style='font-weight:700;'>{decisionWord}</td></tr>
                <tr><td style='color:#6c7870;vertical-align:top;'>Notes</td><td>{safeNotes}</td></tr>
            </table>";

        var intro = accepted
            ? $"The subscription plan change to {System.Net.WebUtility.HtmlEncode(planName)} for {System.Net.WebUtility.HtmlEncode(organizationName)} has been accepted and is now active. Your organization quotas and billing history have been updated."
            : $"The proposed subscription plan change to {System.Net.WebUtility.HtmlEncode(planName)} for {System.Net.WebUtility.HtmlEncode(organizationName)} was declined. Your existing subscription remains unchanged.";

        var html = BuildBrandedEmailTemplate(
            headerTitle: $"Subscription Plan {decisionWord}",
            headerEyebrow: "SUBSCRIPTION CONFIRMATION",
            greeting: $"Hello {System.Net.WebUtility.HtmlEncode(organizationName)} Team,",
            introParagraph: intro,
            detailsBox: detailsBox,
            ctaButtonText: "View Subscription & History",
            ctaUrl: subscriptionPortalUrl,
            footerNote: "You can view your complete plan history anytime under Management → Subscription & Plans.");

        return await SendEmailAsync(
            toAddress: orgAdminEmail,
            subject: subject,
            htmlBody: html,
            notificationType: accepted ? "PlanChangeAccepted" : "PlanChangeDeclined",
            tenantId: tenantId,
            metadata: new { organizationName, planName, accepted },
            ccAddress: platformAdminEmail,
            ct: ct);
    }
}
