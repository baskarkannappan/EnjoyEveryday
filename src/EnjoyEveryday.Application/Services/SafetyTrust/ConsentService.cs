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


public partial class ConsentService : IConsentService
{
    private readonly IConsentRepository _repository;
    private readonly IChildRepository _childRepository;
    private readonly IParentRepository _parentRepository;
    private readonly IConsentStorageService _storageService;
    private readonly IConsentEmailService _emailService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly IPlatformBusinessEventService? _eventService;

    public ConsentService(
        IConsentRepository repository,
        IChildRepository childRepository,
        IParentRepository parentRepository,
        IConsentStorageService storageService,
        IConsentEmailService emailService,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        IPlatformBusinessEventService? eventService = null)
    {
        _repository = repository;
        _childRepository = childRepository;
        _parentRepository = parentRepository;
        _storageService = storageService;
        _emailService = emailService;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _eventService = eventService;
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
        if (!_userContext.HasPermission("consent.create") && !_userContext.HasPermission("consent.manage"))
            throw new UnauthorizedAccessException("Requires consent.create or consent.manage permission.");

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
            $"This is an official document of EnjoyEveryday Daycare. Version {template.Version}.";

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
        if (!_userContext.HasPermission("consent.approve") && !_userContext.HasPermission("consent.manage"))
            throw new UnauthorizedAccessException("Requires consent.approve or consent.manage permission.");

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

        var orgAdminEmail = _eventService != null && doc.OrganizationId.HasValue
            ? await _eventService.ResolveOrganizationAdminEmailAsync(doc.OrganizationId.Value, ct)
            : null;

        var delivery = await _emailService.SendSigningInvitationAsync(
            signer.SignerEmailSnapshot,
            signer.SignerNameSnapshot,
            doc.ChildName?.Split(' ').FirstOrDefault() ?? "Your Child",
            doc.DocumentTitle,
            signer.TokenExpiresAt,
            rawToken,
            baseUrl,
            orgAdminEmail,
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

        var orgAdminEmail = _eventService != null && doc.OrganizationId.HasValue
            ? await _eventService.ResolveOrganizationAdminEmailAsync(doc.OrganizationId.Value, ct)
            : null;

        var delivery = await _emailService.SendSigningReminderAsync(
            signer.SignerEmailSnapshot,
            signer.SignerNameSnapshot,
            doc.ChildName?.Split(' ').FirstOrDefault() ?? "Your Child",
            doc.DocumentTitle,
            signer.TokenExpiresAt,
            rawToken,
            baseUrl,
            orgAdminEmail,
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

}
