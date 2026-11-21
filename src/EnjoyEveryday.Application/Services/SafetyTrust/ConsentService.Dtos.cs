using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public class CreateConsentDraftRequest
{
    public Guid ChildId { get; set; }
    public Guid ConsentTemplateId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ClassroomId { get; set; }
    public DateTime? SigningDeadline { get; set; }
    public string? CustomNotes { get; set; }
}

public class AdminReviewRequest
{
    public Guid DocumentId { get; set; }
    public bool Approve { get; set; }
    public string? ReviewNotes { get; set; }
}

public class ParentSigningSession
{
    public Guid DocumentId { get; set; }
    public Guid SignerId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string DocumentSnapshot { get; set; } = string.Empty;
    public string TemplateVersion { get; set; } = "1.0";
    public string ChildName { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public string SignerEmail { get; set; } = string.Empty;
    public string SignerRelationship { get; set; } = string.Empty;
    public DateTime? SigningDeadline { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<ConsentChoice> Choices { get; set; } = new();
    public bool IsExpired { get; set; }
    public bool IsAlreadySigned { get; set; }
    public bool IsInvalid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ActiveRawToken { get; set; }
}

public class SubmitSignatureRequest
{
    public string RawToken { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public string TypedLegalName { get; set; } = string.Empty;
    public bool LegalIntentConfirmed { get; set; }
    public Dictionary<string, string> Choices { get; set; } = new();
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class ConsentDashboardKpi
{
    public int AwaitingAdminReview { get; set; }
    public int AwaitingParentSignature { get; set; }
    public int Completed { get; set; }
    public int ExpiredOrActionRequired { get; set; }
    public double AverageDaysToSign { get; set; }
}

public interface IConsentService
{
    Task<IEnumerable<ConsentTemplate>> GetTemplatesAsync(CancellationToken ct = default);
    Task<IEnumerable<ChildConsentDocument>> GetDocumentsAsync(Guid? organizationId = null, Guid? branchId = null, Guid? childId = null, string? status = null, CancellationToken ct = default);
    Task<ChildConsentDocument?> GetDocumentByIdAsync(Guid documentId, CancellationToken ct = default);
    Task<ConsentDashboardKpi> GetDashboardKpisAsync(Guid? organizationId = null, CancellationToken ct = default);

    Task<ChildConsentDocument> CreateDraftAsync(CreateConsentDraftRequest request, CancellationToken ct = default);
    Task<ChildConsentDocument> SubmitForReviewAsync(Guid documentId, CancellationToken ct = default);
    Task<ChildConsentDocument> AdminReviewAsync(AdminReviewRequest request, CancellationToken ct = default);
    Task<(bool success, string? rawToken, string? message)> SendInvitationAsync(Guid documentId, string baseUrl, CancellationToken ct = default);
    Task<(bool success, string? message)> SendReminderAsync(Guid documentId, string baseUrl, CancellationToken ct = default);

    Task<ParentSigningSession> GetSigningSessionByTokenAsync(string rawToken, CancellationToken ct = default);
    Task<ParentSigningSession> GetSigningSessionForParentAsync(Guid documentId, CancellationToken ct = default);
    Task<(bool success, string? message)> SignDocumentAsync(SubmitSignatureRequest request, CancellationToken ct = default);
    Task<(bool success, string? message)> DeclineDocumentAsync(string rawToken, string reason, CancellationToken ct = default);
    Task<ChildConsentDocument> WithdrawConsentAsync(Guid documentId, string reason, CancellationToken ct = default);
    Task<ChildConsentDocument> SupersedeDocumentAsync(Guid documentId, Guid newDocumentId, CancellationToken ct = default);

    Task<SignedConsentEvidenceCertificate?> GetSignedCertificateAsync(Guid documentId, CancellationToken ct = default);
}

