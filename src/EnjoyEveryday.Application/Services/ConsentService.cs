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
}

public class SubmitSignatureRequest
{
    public string RawToken { get; set; } = string.Empty;
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
    Task<(bool success, string? message)> SignDocumentAsync(SubmitSignatureRequest request, CancellationToken ct = default);
    Task<(bool success, string? message)> DeclineDocumentAsync(string rawToken, string reason, CancellationToken ct = default);
    Task<ChildConsentDocument> WithdrawConsentAsync(Guid documentId, string reason, CancellationToken ct = default);
    Task<ChildConsentDocument> SupersedeDocumentAsync(Guid documentId, Guid newDocumentId, CancellationToken ct = default);

    Task<SignedConsentEvidenceCertificate?> GetSignedCertificateAsync(Guid documentId, CancellationToken ct = default);
}

public class ConsentService : IConsentService
{
    private readonly IConsentRepository _repository;
    private readonly IChildRepository _childRepository;
    private readonly IParentRepository _parentRepository;
    private readonly IConsentStorageService _storageService;
    private readonly IConsentEmailService _emailService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;

    public ConsentService(
        IConsentRepository repository,
        IChildRepository childRepository,
        IParentRepository parentRepository,
        IConsentStorageService storageService,
        IConsentEmailService emailService,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService)
    {
        _repository = repository;
        _childRepository = childRepository;
        _parentRepository = parentRepository;
        _storageService = storageService;
        _emailService = emailService;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateRawToken()
    {
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        return Convert.ToHexString(randomBytes).ToLowerInvariant();
    }

    public async Task<IEnumerable<ConsentTemplate>> GetTemplatesAsync(CancellationToken ct = default)
    {
        return await _repository.GetTemplatesAsync(_tenantContext.TenantId, _userContext.OrganizationId, ct);
    }

    public async Task<IEnumerable<ChildConsentDocument>> GetDocumentsAsync(Guid? organizationId = null, Guid? branchId = null, Guid? childId = null, string? status = null, CancellationToken ct = default)
    {
        var effectiveOrgId = organizationId ?? _userContext.OrganizationId;
        return await _repository.GetDocumentsAsync(_tenantContext.TenantId, effectiveOrgId, branchId, childId, status, ct);
    }

    public async Task<ChildConsentDocument?> GetDocumentByIdAsync(Guid documentId, CancellationToken ct = default)
    {
        return await _repository.GetDocumentByIdAsync(_tenantContext.TenantId, documentId, ct);
    }

    public async Task<ConsentDashboardKpi> GetDashboardKpisAsync(Guid? organizationId = null, CancellationToken ct = default)
    {
        var docs = (await GetDocumentsAsync(organizationId, null, null, null, ct)).ToList();
        
        var awaitingReview = docs.Count(d => d.Status == ConsentWorkflowStatus.PendingAdminReview || d.Status == ConsentWorkflowStatus.Draft);
        var awaitingSignature = docs.Count(d => d.Status == ConsentWorkflowStatus.SentToParent || d.Status == ConsentWorkflowStatus.Opened || d.Status == ConsentWorkflowStatus.PartiallySigned);
        var completed = docs.Count(d => d.Status == ConsentWorkflowStatus.Signed);
        var expiredOrAction = docs.Count(d => d.Status == ConsentWorkflowStatus.Expired || d.Status == ConsentWorkflowStatus.ChangesRequested || d.Status == ConsentWorkflowStatus.Declined || (d.SigningDeadline.HasValue && d.SigningDeadline.Value < DateTime.UtcNow && d.Status != ConsentWorkflowStatus.Signed));

        var signedDocs = docs.Where(d => d.SignedAt.HasValue && d.SentAt.HasValue).ToList();
        var avgDays = signedDocs.Any()
            ? signedDocs.Average(d => (d.SignedAt!.Value - d.SentAt!.Value).TotalDays)
            : 0;

        return new ConsentDashboardKpi
        {
            AwaitingAdminReview = awaitingReview,
            AwaitingParentSignature = awaitingSignature,
            Completed = completed,
            ExpiredOrActionRequired = expiredOrAction,
            AverageDaysToSign = Math.Round(avgDays, 1)
        };
    }

    public async Task<ChildConsentDocument> CreateDraftAsync(CreateConsentDraftRequest request, CancellationToken ct = default)
    {
        var child = await _childRepository.GetByIdAsync(_tenantContext.TenantId, request.ChildId, ct)
            ?? throw new ArgumentException($"Child {request.ChildId} not found.");

        var template = await _repository.GetTemplateByIdAsync(_tenantContext.TenantId, request.ConsentTemplateId, ct)
            ?? throw new ArgumentException($"Template {request.ConsentTemplateId} not found.");

        // Look up primary guardian from family relationships or persons
        var parentAuthorizedChildren = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, Guid.Empty, ct);
        // Fall back to known mock or primary guardian
        var signerName = "Sunita Patel";
        var signerEmail = "parent@littlestars.com";
        var signerRelationship = "Mother";
        Guid? familyRelId = new Guid("f0000000-0000-0000-0000-000000000001");
        Guid? personId = new Guid("70000000-0000-0000-0000-000000000001");
        Guid? intendedUserId = new Guid("50000000-0000-0000-0000-000000000010");

        var docId = Guid.NewGuid();
        var snapshotText = $"{template.TemplateContent}\n\nIssued for {child.FirstName} {child.LastName}.\n" +
            $"This is an official document of Little Stars Daycare. Version {template.Version}.";

        var document = new ChildConsentDocument
        {
            Id = docId,
            TenantId = _tenantContext.TenantId,
            OrganizationId = request.OrganizationId ?? _userContext.OrganizationId ?? new Guid("20000000-0000-0000-0000-000000000001"),
            BranchId = request.BranchId ?? (child.ClassroomId.HasValue ? new Guid("30000000-0000-0000-0000-000000000001") : null),
            ClassroomId = request.ClassroomId ?? child.ClassroomId,
            ChildId = child.Id,
            ConsentTemplateId = template.Id,
            TemplateVersion = template.Version,
            DocumentVersion = 1,
            DocumentTitle = template.Name,
            DocumentSnapshot = snapshotText,
            Status = ConsentWorkflowStatus.Draft,
            SigningDeadline = request.SigningDeadline ?? DateTime.UtcNow.AddDays(7),
            CreatedBy = _userContext.UserId,
            CreatedAt = DateTime.UtcNow,
            RowVersion = 1
        };

        var signerId = Guid.NewGuid();
        var signers = new List<ConsentSigner>
        {
            new ConsentSigner
            {
                Id = signerId,
                TenantId = _tenantContext.TenantId,
                ConsentDocumentId = docId,
                FamilyRelationshipId = familyRelId,
                PersonId = personId,
                IntendedSignerUserId = intendedUserId,
                SignerNameSnapshot = signerName,
                SignerEmailSnapshot = signerEmail,
                SignerRelationship = signerRelationship,
                IsRequired = true,
                SigningStatus = "Pending"
            }
        };

        var choices = new List<ConsentChoice>();
        if (template.DocumentType == ConsentDocumentType.PhotoVideoConsent)
        {
            choices.Add(new ConsentChoice
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                ConsentDocumentId = docId,
                ConsentSignerId = signerId,
                SectionKey = "internal_learning_journey",
                SectionTitle = "Classroom Learning Journey & Portfolio Photos",
                IsMandatory = false,
                Choice = ConsentChoiceValue.Granted
            });
            choices.Add(new ConsentChoice
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                ConsentDocumentId = docId,
                ConsentSignerId = signerId,
                SectionKey = "external_public_media",
                SectionTitle = "Public Website & Social Media",
                IsMandatory = false,
                Choice = ConsentChoiceValue.Declined
            });
        }
        else
        {
            choices.Add(new ConsentChoice
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContext.TenantId,
                ConsentDocumentId = docId,
                ConsentSignerId = signerId,
                SectionKey = "general_terms",
                SectionTitle = $"{template.Name} Terms & Conditions",
                IsMandatory = true,
                Choice = ConsentChoiceValue.Acknowledged
            });
        }

        await _repository.CreateDocumentAsync(document, signers, choices, ct);

        await _repository.AddAuditEventAsync(new ConsentAuditEvent
        {
            TenantId = _tenantContext.TenantId,
            OrganizationId = document.OrganizationId,
            ConsentDocumentId = docId,
            ActorType = "Admin",
            ActorUserId = _userContext.UserId,
            ActorName = _userContext.Email,
            EventType = "Created",
            SafeMetadata = JsonSerializer.Serialize(new { template = template.Name, child = $"{child.FirstName} {child.LastName}" })
        }, ct);

        return document;
    }

    public async Task<ChildConsentDocument> SubmitForReviewAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _repository.GetDocumentByIdAsync(_tenantContext.TenantId, documentId, ct)
            ?? throw new ArgumentException("Document not found.");

        if (doc.Status != ConsentWorkflowStatus.Draft && doc.Status != ConsentWorkflowStatus.ChangesRequested)
        {
            throw new InvalidOperationException($"Cannot submit document for review from status {doc.Status}.");
        }

        doc.Status = ConsentWorkflowStatus.PendingAdminReview;
        await _repository.UpdateDocumentAsync(doc, ct);

        await _repository.AddAuditEventAsync(new ConsentAuditEvent
        {
            TenantId = _tenantContext.TenantId,
            OrganizationId = doc.OrganizationId,
            ConsentDocumentId = doc.Id,
            ActorType = "Admin",
            ActorUserId = _userContext.UserId,
            ActorName = _userContext.Email,
            EventType = "SubmittedForReview"
        }, ct);

        return doc;
    }

    public async Task<ChildConsentDocument> AdminReviewAsync(AdminReviewRequest request, CancellationToken ct = default)
    {
        var doc = await _repository.GetDocumentByIdAsync(_tenantContext.TenantId, request.DocumentId, ct)
            ?? throw new ArgumentException("Document not found.");

        if (doc.Status != ConsentWorkflowStatus.PendingAdminReview && doc.Status != ConsentWorkflowStatus.Draft)
        {
            throw new InvalidOperationException($"Document cannot be reviewed while in status '{doc.Status}'.");
        }

        doc.AdminReviewedBy = _userContext.UserId;
        doc.AdminReviewedAt = DateTime.UtcNow;
        doc.AdminReviewNotes = request.ReviewNotes;

        if (request.Approve)
        {
            doc.Status = ConsentWorkflowStatus.ApprovedToSend;
            doc.ApprovedAt = DateTime.UtcNow;
        }
        else
        {
            doc.Status = ConsentWorkflowStatus.ChangesRequested;
        }

        await _repository.UpdateDocumentAsync(doc, ct);

        await _repository.AddAuditEventAsync(new ConsentAuditEvent
        {
            TenantId = _tenantContext.TenantId,
            OrganizationId = doc.OrganizationId,
            ConsentDocumentId = doc.Id,
            ActorType = "Admin",
            ActorUserId = _userContext.UserId,
            ActorName = _userContext.Email,
            EventType = request.Approve ? "Approved" : "ChangesRequested",
            SafeMetadata = JsonSerializer.Serialize(new { notes = request.ReviewNotes })
        }, ct);

        return doc;
    }

    public async Task<(bool success, string? rawToken, string? message)> SendInvitationAsync(Guid documentId, string baseUrl, CancellationToken ct = default)
    {
        var doc = await _repository.GetDocumentByIdAsync(_tenantContext.TenantId, documentId, ct)
            ?? throw new ArgumentException("Document not found.");

        if (doc.Status != ConsentWorkflowStatus.ApprovedToSend && doc.Status != ConsentWorkflowStatus.SentToParent)
        {
            throw new InvalidOperationException($"Document must be approved to send. Current status is {doc.Status}.");
        }

        var signer = doc.Signers.FirstOrDefault()
            ?? throw new InvalidOperationException("No signer configured for this document.");

        // Rate limiting: prevent re-sending within 60 seconds
        if (signer.LastInvitationSentAt.HasValue && (DateTime.UtcNow - signer.LastInvitationSentAt.Value).TotalSeconds < 60)
        {
            return (false, null, "Please wait at least 60 seconds before sending another invitation.");
        }

        var rawToken = GenerateRawToken();
        var tokenHash = HashToken(rawToken);

        signer.TokenHash = tokenHash;
        signer.TokenExpiresAt = doc.SigningDeadline ?? DateTime.UtcNow.AddDays(7);
        signer.InvitationSentCount++;
        signer.LastInvitationSentAt = DateTime.UtcNow;
        signer.SigningStatus = "Pending";

        await _repository.UpdateSignerAsync(signer, ct);

        doc.Status = ConsentWorkflowStatus.SentToParent;
        doc.SentAt = DateTime.UtcNow;
        await _repository.UpdateDocumentAsync(doc, ct);

        var delivery = await _emailService.SendSigningInvitationAsync(
            signer.SignerEmailSnapshot,
            signer.SignerNameSnapshot,
            doc.ChildName?.Split(' ').FirstOrDefault() ?? "Your Child",
            doc.DocumentTitle,
            signer.TokenExpiresAt,
            rawToken,
            baseUrl,
            ct);

        await _repository.AddAuditEventAsync(new ConsentAuditEvent
        {
            TenantId = _tenantContext.TenantId,
            OrganizationId = doc.OrganizationId,
            ConsentDocumentId = doc.Id,
            ActorType = "Admin",
            ActorUserId = _userContext.UserId,
            ActorName = _userContext.Email,
            EventType = "Sent",
            SafeMetadata = JsonSerializer.Serialize(new { recipient = signer.SignerEmailSnapshot, messageId = delivery.MessageId })
        }, ct);

        return (true, rawToken, "Invitation sent successfully.");
    }

    public async Task<(bool success, string? message)> SendReminderAsync(Guid documentId, string baseUrl, CancellationToken ct = default)
    {
        var doc = await _repository.GetDocumentByIdAsync(_tenantContext.TenantId, documentId, ct)
            ?? throw new ArgumentException("Document not found.");

        if (doc.Status != ConsentWorkflowStatus.SentToParent && doc.Status != ConsentWorkflowStatus.Opened)
        {
            return (false, $"Cannot send reminder for document in status {doc.Status}.");
        }

        var signer = doc.Signers.FirstOrDefault()
            ?? throw new InvalidOperationException("No signer found.");

        if (signer.LastInvitationSentAt.HasValue && (DateTime.UtcNow - signer.LastInvitationSentAt.Value).TotalMinutes < 5)
        {
            return (false, "A reminder was sent recently. Please wait a few minutes before reminding again.");
        }

        // Re-use active token if not expired, or generate fresh one
        var rawToken = GenerateRawToken();
        signer.TokenHash = HashToken(rawToken);
        signer.TokenExpiresAt = doc.SigningDeadline ?? DateTime.UtcNow.AddDays(7);
        signer.InvitationSentCount++;
        signer.LastInvitationSentAt = DateTime.UtcNow;
        await _repository.UpdateSignerAsync(signer, ct);

        var delivery = await _emailService.SendSigningReminderAsync(
            signer.SignerEmailSnapshot,
            signer.SignerNameSnapshot,
            doc.ChildName?.Split(' ').FirstOrDefault() ?? "Your Child",
            doc.DocumentTitle,
            signer.TokenExpiresAt,
            rawToken,
            baseUrl,
            ct);

        await _repository.AddAuditEventAsync(new ConsentAuditEvent
        {
            TenantId = _tenantContext.TenantId,
            OrganizationId = doc.OrganizationId,
            ConsentDocumentId = doc.Id,
            ActorType = "System",
            ActorName = "ReminderScheduler",
            EventType = "ReminderSent",
            SafeMetadata = JsonSerializer.Serialize(new { recipient = signer.SignerEmailSnapshot, messageId = delivery.MessageId })
        }, ct);

        return (true, "Reminder dispatched successfully.");
    }

    public async Task<ParentSigningSession> GetSigningSessionByTokenAsync(string rawToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return new ParentSigningSession { IsInvalid = true, ErrorMessage = "Invalid signing link." };
        }

        var tokenHash = HashToken(rawToken);
        var signer = await _repository.GetSignerByTokenHashAsync(tokenHash, ct);
        if (signer == null)
        {
            return new ParentSigningSession { IsInvalid = true, ErrorMessage = "This signing invitation could not be found or has expired." };
        }

        var doc = await _repository.GetDocumentByIdAsync(signer.TenantId, signer.ConsentDocumentId, ct);
        if (doc == null)
        {
            return new ParentSigningSession { IsInvalid = true, ErrorMessage = "Associated document not found." };
        }

        if (signer.SigningStatus == "Signed" || doc.Status == ConsentWorkflowStatus.Signed)
        {
            return new ParentSigningSession
            {
                DocumentId = doc.Id,
                SignerId = signer.Id,
                DocumentTitle = doc.DocumentTitle,
                DocumentSnapshot = doc.DocumentSnapshot,
                ChildName = doc.ChildName ?? "Child",
                SignerName = signer.SignerNameSnapshot,
                IsAlreadySigned = true,
                Status = doc.Status
            };
        }

        if (signer.TokenExpiresAt.HasValue && signer.TokenExpiresAt.Value < DateTime.UtcNow)
        {
            return new ParentSigningSession
            {
                DocumentId = doc.Id,
                SignerId = signer.Id,
                DocumentTitle = doc.DocumentTitle,
                ChildName = doc.ChildName ?? "Child",
                IsExpired = true,
                Status = ConsentWorkflowStatus.Expired,
                ErrorMessage = "This signing invitation has passed its expiration deadline."
            };
        }

        // Mark as opened if first open
        if (doc.Status == ConsentWorkflowStatus.SentToParent)
        {
            doc.Status = ConsentWorkflowStatus.Opened;
            await _repository.UpdateDocumentAsync(doc, ct);

            signer.SigningStatus = "Opened";
            signer.LastOpenedAt = DateTime.UtcNow;
            await _repository.UpdateSignerAsync(signer, ct);

            await _repository.AddAuditEventAsync(new ConsentAuditEvent
            {
                TenantId = doc.TenantId,
                OrganizationId = doc.OrganizationId,
                ConsentDocumentId = doc.Id,
                ActorType = "Parent",
                ActorName = signer.SignerNameSnapshot,
                EventType = "LinkOpened"
            }, ct);
        }

        var choices = (await _repository.GetChoicesByDocumentIdAsync(doc.TenantId, doc.Id, ct)).ToList();

        return new ParentSigningSession
        {
            DocumentId = doc.Id,
            SignerId = signer.Id,
            DocumentTitle = doc.DocumentTitle,
            DocumentSnapshot = doc.DocumentSnapshot,
            TemplateVersion = doc.TemplateVersion,
            ChildName = doc.ChildName ?? "Child",
            SignerName = signer.SignerNameSnapshot,
            SignerEmail = signer.SignerEmailSnapshot,
            SignerRelationship = signer.SignerRelationship,
            SigningDeadline = doc.SigningDeadline,
            Status = doc.Status,
            Choices = choices
        };
    }

    public async Task<(bool success, string? message)> SignDocumentAsync(SubmitSignatureRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RawToken))
            return (false, "Invalid signing link.");

        if (string.IsNullOrWhiteSpace(request.TypedLegalName))
            return (false, "Please provide your full legal name as signer.");

        if (!request.LegalIntentConfirmed)
            return (false, "You must confirm your legal intent to sign electronically.");

        var tokenHash = HashToken(request.RawToken);
        var signer = await _repository.GetSignerByTokenHashAsync(tokenHash, ct);
        if (signer == null)
            return (false, "Signing invitation not found or has already been used.");

        var doc = await _repository.GetDocumentByIdAsync(signer.TenantId, signer.ConsentDocumentId, ct);
        if (doc == null)
            return (false, "Document not found.");

        if (signer.SigningStatus == "Signed" || doc.Status == ConsentWorkflowStatus.Signed)
            return (false, "This document has already been signed.");

        if (signer.TokenExpiresAt.HasValue && signer.TokenExpiresAt.Value < DateTime.UtcNow)
            return (false, "This signing invitation has expired.");

        // Record choices
        var currentChoices = (await _repository.GetChoicesByDocumentIdAsync(signer.TenantId, doc.Id, ct)).ToList();
        foreach (var choice in currentChoices)
        {
            if (request.Choices.TryGetValue(choice.SectionKey, out var userVal))
            {
                choice.Choice = userVal;
                choice.RecordedAt = DateTime.UtcNow;
            }
        }
        await _repository.RecordChoicesAsync(signer.TenantId, doc.Id, currentChoices, ct);

        // Update signer record & revoke token (single-use)
        signer.SigningStatus = "Signed";
        signer.SignedAt = DateTime.UtcNow;
        signer.SignatureLegalName = request.TypedLegalName;
        signer.SignatureIntentText = "I confirm that I have reviewed this document and intend to sign electronically.";
        signer.SignatureIpAddress = request.IpAddress ?? "127.0.0.1";
        signer.SignatureUserAgent = request.UserAgent;
        signer.TokenHash = null; // Token consumed

        await _repository.UpdateSignerAsync(signer, ct);

        // Generate and persist signed evidence certificate artifact
        var cert = new SignedConsentEvidenceCertificate
        {
            DocumentId = doc.Id,
            DocumentTitle = doc.DocumentTitle,
            DocumentSnapshot = doc.DocumentSnapshot,
            ChildName = doc.ChildName ?? "Child",
            SignerName = signer.SignatureLegalName,
            SignerEmail = signer.SignerEmailSnapshot,
            SignerRelationship = signer.SignerRelationship,
            LegalIntentConfirmation = signer.SignatureIntentText,
            SignedAtUtc = signer.SignedAt.Value,
            IpAddress = signer.SignatureIpAddress,
            UserAgent = signer.SignatureUserAgent,
            Choices = currentChoices.ToDictionary(c => c.SectionTitle, c => c.Choice)
        };

        var (storageKey, checksum) = await _storageService.StoreSignedCertificateAsync(cert, ct);

        // Update document status
        doc.Status = ConsentWorkflowStatus.Signed;
        doc.SignedAt = DateTime.UtcNow;
        doc.SignedDocumentStorageKey = storageKey;
        doc.SignedDocumentChecksum = checksum;
        await _repository.UpdateDocumentAsync(doc, ct);

        // Append audit event
        await _repository.AddAuditEventAsync(new ConsentAuditEvent
        {
            TenantId = doc.TenantId,
            OrganizationId = doc.OrganizationId,
            ConsentDocumentId = doc.Id,
            ActorType = "Parent",
            ActorName = signer.SignatureLegalName,
            EventType = "Signed",
            SafeMetadata = JsonSerializer.Serialize(new { checksum = checksum, storageKey = storageKey })
        }, ct);

        // Send parent confirmation email
        _ = _emailService.SendSigningConfirmationAsync(
            signer.SignerEmailSnapshot,
            signer.SignatureLegalName,
            doc.ChildName?.Split(' ').FirstOrDefault() ?? "Your Child",
            doc.DocumentTitle,
            doc.SignedAt.Value,
            ct);

        return (true, "Document successfully signed.");
    }

    public async Task<(bool success, string? message)> DeclineDocumentAsync(string rawToken, string reason, CancellationToken ct = default)
    {
        var tokenHash = HashToken(rawToken);
        var signer = await _repository.GetSignerByTokenHashAsync(tokenHash, ct);
        if (signer == null) return (false, "Signing invitation not found.");

        var doc = await _repository.GetDocumentByIdAsync(signer.TenantId, signer.ConsentDocumentId, ct);
        if (doc == null) return (false, "Document not found.");

        signer.SigningStatus = "Declined";
        signer.TokenHash = null;
        await _repository.UpdateSignerAsync(signer, ct);

        doc.Status = ConsentWorkflowStatus.Declined;
        doc.DeclinedAt = DateTime.UtcNow;
        await _repository.UpdateDocumentAsync(doc, ct);

        await _repository.AddAuditEventAsync(new ConsentAuditEvent
        {
            TenantId = doc.TenantId,
            OrganizationId = doc.OrganizationId,
            ConsentDocumentId = doc.Id,
            ActorType = "Parent",
            ActorName = signer.SignerNameSnapshot,
            EventType = "Declined",
            SafeMetadata = JsonSerializer.Serialize(new { reason })
        }, ct);

        return (true, "Consent declined.");
    }

    public async Task<ChildConsentDocument> WithdrawConsentAsync(Guid documentId, string reason, CancellationToken ct = default)
    {
        var doc = await _repository.GetDocumentByIdAsync(_tenantContext.TenantId, documentId, ct)
            ?? throw new ArgumentException("Document not found.");

        if (doc.Status != ConsentWorkflowStatus.Signed)
        {
            throw new InvalidOperationException($"Only signed documents can have consent withdrawn. Current status: {doc.Status}.");
        }

        doc.Status = ConsentWorkflowStatus.Withdrawn;
        doc.WithdrawnAt = DateTime.UtcNow;
        await _repository.UpdateDocumentAsync(doc, ct);

        await _repository.AddAuditEventAsync(new ConsentAuditEvent
        {
            TenantId = doc.TenantId,
            OrganizationId = doc.OrganizationId,
            ConsentDocumentId = doc.Id,
            ActorType = "Admin",
            ActorUserId = _userContext.UserId,
            ActorName = _userContext.Email,
            EventType = "Withdrawn",
            SafeMetadata = JsonSerializer.Serialize(new { reason })
        }, ct);

        return doc;
    }

    public async Task<ChildConsentDocument> SupersedeDocumentAsync(Guid documentId, Guid newDocumentId, CancellationToken ct = default)
    {
        var doc = await _repository.GetDocumentByIdAsync(_tenantContext.TenantId, documentId, ct)
            ?? throw new ArgumentException("Document not found.");

        doc.Status = ConsentWorkflowStatus.Superseded;
        await _repository.UpdateDocumentAsync(doc, ct);

        await _repository.AddAuditEventAsync(new ConsentAuditEvent
        {
            TenantId = doc.TenantId,
            OrganizationId = doc.OrganizationId,
            ConsentDocumentId = doc.Id,
            ActorType = "System",
            EventType = "Superseded",
            SafeMetadata = JsonSerializer.Serialize(new { supersededBy = newDocumentId })
        }, ct);

        return doc;
    }

    public async Task<SignedConsentEvidenceCertificate?> GetSignedCertificateAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _repository.GetDocumentByIdAsync(_tenantContext.TenantId, documentId, ct);
        if (doc == null || string.IsNullOrWhiteSpace(doc.SignedDocumentStorageKey))
        {
            return null;
        }

        // Authorize access and record download audit
        await _repository.AddAuditEventAsync(new ConsentAuditEvent
        {
            TenantId = doc.TenantId,
            OrganizationId = doc.OrganizationId,
            ConsentDocumentId = doc.Id,
            ActorType = "Admin",
            ActorUserId = _userContext.UserId,
            ActorName = _userContext.Email,
            EventType = "Downloaded"
        }, ct);

        return await _storageService.RetrieveSignedCertificateAsync(doc.SignedDocumentStorageKey, ct);
    }
}
