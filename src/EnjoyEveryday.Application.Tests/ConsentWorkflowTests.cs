using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace EnjoyEveryday.Application.Tests;

[TestFixture]
public class ConsentWorkflowTests
{
    private class MockUserContext : IUserContext
    {
        public Guid UserId { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = "admin@littlestars.com";
        public bool IsAuthenticated { get; set; } = true;
        public IEnumerable<string> Roles { get; set; } = new[] { "DaycareAdmin" };
        public IEnumerable<string> Permissions { get; set; } = new[] { "consent.manage", "safety.view" };
        public Guid? OrganizationId { get; set; } = Guid.NewGuid();

        public bool HasPermission(string permission) => Permissions.Contains(permission);
    }

    private class MockTenantContext : ITenantContext
    {
        public Guid TenantId { get; set; } = Guid.NewGuid();
        public string TenantName { get; set; } = "Little Stars Preschool";
        public bool IsResolved { get; set; } = true;
    }

    private class InMemoryConsentRepository : IConsentRepository
    {
        public List<ConsentTemplate> Templates = new();
        public List<ChildConsentDocument> Documents = new();
        public List<ConsentSigner> Signers = new();
        public List<ConsentChoice> Choices = new();
        public List<ConsentAuditEvent> AuditEvents = new();

        public Task<IEnumerable<ConsentTemplate>> GetTemplatesAsync(Guid tenantId, Guid? organizationId = null, CancellationToken ct = default)
        {
            var res = Templates.Where(t => t.TenantId == tenantId && (organizationId == null || t.OrganizationId == null || t.OrganizationId == organizationId));
            return Task.FromResult<IEnumerable<ConsentTemplate>>(res.ToList());
        }

        public Task<ConsentTemplate?> GetTemplateByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        {
            return Task.FromResult(Templates.FirstOrDefault(t => t.TenantId == tenantId && t.Id == id));
        }

        public Task<ChildConsentDocument> CreateDocumentAsync(ChildConsentDocument document, List<ConsentSigner> signers, List<ConsentChoice> choices, CancellationToken ct = default)
        {
            Documents.Add(document);
            Signers.AddRange(signers);
            Choices.AddRange(choices);
            document.Signers = signers;
            document.Choices = choices;
            return Task.FromResult(document);
        }

        public Task UpdateDocumentAsync(ChildConsentDocument document, CancellationToken ct = default)
        {
            var idx = Documents.FindIndex(d => d.Id == document.Id && d.TenantId == document.TenantId);
            if (idx >= 0) Documents[idx] = document;
            return Task.CompletedTask;
        }

        public Task<ChildConsentDocument?> GetDocumentByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        {
            var doc = Documents.FirstOrDefault(d => d.TenantId == tenantId && d.Id == id);
            if (doc != null)
            {
                doc.Signers = Signers.Where(s => s.ConsentDocumentId == id).ToList();
                doc.Choices = Choices.Where(c => c.ConsentDocumentId == id).ToList();
                doc.AuditEvents = AuditEvents.Where(a => a.ConsentDocumentId == id).ToList();
            }
            return Task.FromResult(doc);
        }

        public Task<IEnumerable<ChildConsentDocument>> GetDocumentsAsync(Guid tenantId, Guid? organizationId = null, Guid? branchId = null, Guid? childId = null, string? status = null, CancellationToken ct = default)
        {
            var query = Documents.Where(d => d.TenantId == tenantId);
            if (organizationId.HasValue) query = query.Where(d => d.OrganizationId == organizationId);
            if (childId.HasValue) query = query.Where(d => d.ChildId == childId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(d => d.Status == status);

            var list = query.ToList();
            foreach (var doc in list)
            {
                doc.Signers = Signers.Where(s => s.ConsentDocumentId == doc.Id).ToList();
            }
            return Task.FromResult<IEnumerable<ChildConsentDocument>>(list);
        }

        public Task<ConsentSigner?> GetSignerByTokenHashAsync(string tokenHash, CancellationToken ct = default)
        {
            return Task.FromResult(Signers.FirstOrDefault(s => s.TokenHash == tokenHash));
        }

        public Task<ConsentSigner?> GetSignerByIdAsync(Guid tenantId, Guid signerId, CancellationToken ct = default)
        {
            return Task.FromResult(Signers.FirstOrDefault(s => s.TenantId == tenantId && s.Id == signerId));
        }

        public Task UpdateSignerAsync(ConsentSigner signer, CancellationToken ct = default)
        {
            var idx = Signers.FindIndex(s => s.Id == signer.Id);
            if (idx >= 0) Signers[idx] = signer;
            return Task.CompletedTask;
        }

        public Task RecordChoicesAsync(Guid tenantId, Guid documentId, List<ConsentChoice> choices, CancellationToken ct = default)
        {
            Choices.RemoveAll(c => c.TenantId == tenantId && c.ConsentDocumentId == documentId);
            Choices.AddRange(choices);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ConsentChoice>> GetChoicesByDocumentIdAsync(Guid tenantId, Guid documentId, CancellationToken ct = default)
        {
            return Task.FromResult<IEnumerable<ConsentChoice>>(Choices.Where(c => c.TenantId == tenantId && c.ConsentDocumentId == documentId).ToList());
        }

        public Task AddAuditEventAsync(ConsentAuditEvent auditEvent, CancellationToken ct = default)
        {
            AuditEvents.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ConsentAuditEvent>> GetAuditEventsAsync(Guid tenantId, Guid documentId, CancellationToken ct = default)
        {
            return Task.FromResult<IEnumerable<ConsentAuditEvent>>(AuditEvents.Where(a => a.TenantId == tenantId && a.ConsentDocumentId == documentId).ToList());
        }
    }

    private class MockChildRepo : IChildRepository
    {
        public List<Child> Children = new();

        public Task<Child> AddAsync(Child child, CancellationToken ct = default) { Children.Add(child); return Task.FromResult(child); }
        public Task<Child?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
        {
            return Task.FromResult(Children.FirstOrDefault(c => c.TenantId == tenantId && c.Id == id));
        }
        public Task<IEnumerable<Child>> GetAllAsync(Guid tenantId, CancellationToken ct = default) => Task.FromResult<IEnumerable<Child>>(Children.Where(c => c.TenantId == tenantId).ToList());
        public Task<IEnumerable<Child>> GetByClassroomIdAsync(Guid tenantId, Guid classroomId, CancellationToken ct = default) => Task.FromResult<IEnumerable<Child>>(Children.Where(c => c.TenantId == tenantId && c.ClassroomId == classroomId).ToList());
        public Task UpdateAsync(Child child, CancellationToken ct = default) => Task.CompletedTask;
        public Task DeleteAsync(Guid tenantId, Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task SyncParentGuardianAsync(Guid tenantId, Guid childId, string parentEmail, string parentName, string relationship, string phone, CancellationToken ct = default) => Task.CompletedTask;
    }

    private class MockParentRepo : IParentRepository
    {
        public Task<IEnumerable<ParentAuthorizedChild>> GetAuthorizedChildrenAsync(Guid tenantId, Guid userId, CancellationToken ct = default)
        {
            return Task.FromResult<IEnumerable<ParentAuthorizedChild>>(new List<ParentAuthorizedChild>());
        }
        public Task<bool> HasChildAccessAsync(Guid tenantId, Guid userId, Guid childId, CancellationToken ct = default) => Task.FromResult(true);
        public Task<ChildAttendance?> GetChildAttendanceForDateAsync(Guid tenantId, Guid childId, DateTime date, CancellationToken ct = default) => Task.FromResult<ChildAttendance?>(null);
        public Task<IEnumerable<ChildJourneyEntry>> GetApprovedJourneyEntriesAsync(Guid tenantId, Guid childId, CancellationToken ct = default) => Task.FromResult<IEnumerable<ChildJourneyEntry>>(new List<ChildJourneyEntry>());
        public Task<IEnumerable<ChildMilestone>> GetApprovedMilestonesAsync(Guid tenantId, Guid childId, CancellationToken ct = default) => Task.FromResult<IEnumerable<ChildMilestone>>(new List<ChildMilestone>());
        public Task<IEnumerable<Event>> GetApprovedEventsForChildAsync(Guid tenantId, Guid childId, CancellationToken ct = default) => Task.FromResult<IEnumerable<Event>>(new List<Event>());
        public Task<Person?> GetPersonByUserIdAsync(Guid tenantId, Guid userId, CancellationToken ct = default) => Task.FromResult<Person?>(null);
        public Task UpdatePersonContactAsync(Guid tenantId, Guid personId, string? mobilePhone, string? preferredContactMethod, string? preferredLanguage, CancellationToken ct = default) => Task.CompletedTask;
    }

    private class MockAuditService : IAuditService
    {
        public Task LogAsync(AuditEntry entry, CancellationToken ct = default) => Task.CompletedTask;
    }

    private MockTenantContext _tenantContext = null!;
    private MockUserContext _userContext = null!;
    private InMemoryConsentRepository _consentRepo = null!;
    private MockChildRepo _childRepo = null!;
    private MockParentRepo _parentRepo = null!;
    private LocalConsentStorageService _storageService = null!;
    private ConsentEmailService _emailService = null!;
    private ConsentService _service = null!;

    private Guid _childId;
    private Guid _templateId;

    [SetUp]
    public void Setup()
    {
        _tenantContext = new MockTenantContext();
        _userContext = new MockUserContext();
        _consentRepo = new InMemoryConsentRepository();
        _childRepo = new MockChildRepo();
        _parentRepo = new MockParentRepo();
        _storageService = new LocalConsentStorageService();
        _emailService = new ConsentEmailService(NullLogger<ConsentEmailService>.Instance);
        var audit = new MockAuditService();

        _service = new ConsentService(
            _consentRepo, _childRepo, _parentRepo, _storageService, _emailService,
            _tenantContext, _userContext, audit);

        _childId = Guid.NewGuid();
        _childRepo.Children.Add(new Child
        {
            Id = _childId,
            TenantId = _tenantContext.TenantId,
            FirstName = "Aarav",
            LastName = "Patel"
        });

        _templateId = Guid.NewGuid();
        _consentRepo.Templates.Add(new ConsentTemplate
        {
            Id = _templateId,
            TenantId = _tenantContext.TenantId,
            OrganizationId = _userContext.OrganizationId,
            Name = "Photo & Video Consent",
            DocumentType = ConsentDocumentType.PhotoVideoConsent,
            Version = "1.0",
            TemplateContent = "Consent for learning journey portfolio and public media."
        });
    }

    [Test]
    public async Task EndToEndWorkflow_DraftToApprovalToSigning_SucceedsWithAuditAndEvidence()
    {
        // 1. Create Draft
        var createReq = new CreateConsentDraftRequest
        {
            ChildId = _childId,
            ConsentTemplateId = _templateId,
            SigningDeadline = DateTime.UtcNow.AddDays(7)
        };
        var draft = await _service.CreateDraftAsync(createReq);
        Assert.That(draft.Status, Is.EqualTo(ConsentWorkflowStatus.Draft));
        Assert.That(_consentRepo.AuditEvents.Any(a => a.EventType == "Created"), Is.True);

        // 2. Admin Review & Approve
        var reviewReq = new AdminReviewRequest
        {
            DocumentId = draft.Id,
            Approve = true,
            ReviewNotes = "Reviewed guardian records and dates."
        };
        var approved = await _service.AdminReviewAsync(reviewReq);
        Assert.That(approved.Status, Is.EqualTo(ConsentWorkflowStatus.ApprovedToSend));
        Assert.That(_consentRepo.AuditEvents.Any(a => a.EventType == "Approved"), Is.True);

        // 3. Send Invitation Email
        var (sendOk, rawToken, sendMsg) = await _service.SendInvitationAsync(approved.Id, "https://localhost:7003/");
        Assert.That(sendOk, Is.True);
        Assert.That(rawToken, Is.Not.Null.And.Not.Empty);

        var docAfterSend = await _service.GetDocumentByIdAsync(approved.Id);
        Assert.That(docAfterSend!.Status, Is.EqualTo(ConsentWorkflowStatus.SentToParent));
        Assert.That(docAfterSend.Signers.First().TokenHash, Is.Not.Null);

        // 4. Parent Opens Link (Status changes to Opened)
        var session = await _service.GetSigningSessionByTokenAsync(rawToken!);
        Assert.That(session.IsInvalid, Is.False);
        Assert.That(session.Status, Is.EqualTo(ConsentWorkflowStatus.Opened));
        Assert.That(_consentRepo.AuditEvents.Any(a => a.EventType == "LinkOpened"), Is.True);

        // 5. Parent Signs Online with Separate Choices
        var choices = new Dictionary<string, string>
        {
            { "internal_learning_journey", ConsentChoiceValue.Granted },
            { "external_public_media", ConsentChoiceValue.Declined }
        };

        var signReq = new SubmitSignatureRequest
        {
            RawToken = rawToken!,
            TypedLegalName = "Sunita Patel",
            LegalIntentConfirmed = true,
            Choices = choices
        };

        var (signOk, signMsg) = await _service.SignDocumentAsync(signReq);
        Assert.That(signOk, Is.True);

        // 6. Verify Completed Document State & Evidence
        var finalDoc = await _service.GetDocumentByIdAsync(approved.Id);
        Assert.That(finalDoc!.Status, Is.EqualTo(ConsentWorkflowStatus.Signed));
        Assert.That(finalDoc.SignedAt, Is.Not.Null);
        Assert.That(finalDoc.SignedDocumentChecksum, Is.Not.Null);

        // Token is consumed / single-use: token_hash is null
        var signer = finalDoc.Signers.First();
        Assert.That(signer.SigningStatus, Is.EqualTo("Signed"));
        Assert.That(signer.TokenHash, Is.Null);

        // Separate granular choices recorded
        var recordedChoices = await _consentRepo.GetChoicesByDocumentIdAsync(_tenantContext.TenantId, finalDoc.Id);
        var publicMediaChoice = recordedChoices.First(c => c.SectionKey == "external_public_media");
        Assert.That(publicMediaChoice.Choice, Is.EqualTo(ConsentChoiceValue.Declined));

        // Audit Trail contains complete lifecycle
        var audit = await _consentRepo.GetAuditEventsAsync(_tenantContext.TenantId, finalDoc.Id);
        Assert.That(audit.Any(a => a.EventType == "Signed"), Is.True);
    }

    [Test]
    public async Task SingleUseToken_CannotBeReusedAfterSigning()
    {
        var draft = await _service.CreateDraftAsync(new CreateConsentDraftRequest
        {
            ChildId = _childId,
            ConsentTemplateId = _templateId
        });
        await _service.AdminReviewAsync(new AdminReviewRequest { DocumentId = draft.Id, Approve = true });
        var (_, token, _) = await _service.SendInvitationAsync(draft.Id, "https://localhost:7003/");

        // First signature
        var signReq = new SubmitSignatureRequest
        {
            RawToken = token!,
            TypedLegalName = "Sunita Patel",
            LegalIntentConfirmed = true
        };
        var (firstOk, _) = await _service.SignDocumentAsync(signReq);
        Assert.That(firstOk, Is.True);

        // Attempt replay with consumed token
        var (secondOk, secondMsg) = await _service.SignDocumentAsync(signReq);
        Assert.That(secondOk, Is.False);
        Assert.That(secondMsg, Does.Contain("not found or has already been used"));
    }

    [Test]
    public async Task ExpiredToken_IsRejected()
    {
        var draft = await _service.CreateDraftAsync(new CreateConsentDraftRequest
        {
            ChildId = _childId,
            ConsentTemplateId = _templateId,
            SigningDeadline = DateTime.UtcNow.AddHours(-1) // Already expired
        });
        await _service.AdminReviewAsync(new AdminReviewRequest { DocumentId = draft.Id, Approve = true });
        var (_, token, _) = await _service.SendInvitationAsync(draft.Id, "https://localhost:7003/");

        var session = await _service.GetSigningSessionByTokenAsync(token!);
        Assert.That(session.IsExpired, Is.True);

        var (signOk, signMsg) = await _service.SignDocumentAsync(new SubmitSignatureRequest
        {
            RawToken = token!,
            TypedLegalName = "Sunita Patel",
            LegalIntentConfirmed = true
        });
        Assert.That(signOk, Is.False);
        Assert.That(signMsg, Does.Contain("expired"));
    }

    [Test]
    public async Task MultiTenantIsolation_CrossTenantAccessBlocked()
    {
        var otherTenantId = Guid.NewGuid();
        var draft = await _service.CreateDraftAsync(new CreateConsentDraftRequest
        {
            ChildId = _childId,
            ConsentTemplateId = _templateId
        });

        // Query with another tenant ID
        var crossTenantDoc = await _consentRepo.GetDocumentByIdAsync(otherTenantId, draft.Id);
        Assert.That(crossTenantDoc, Is.Null);
    }

    [Test]
    public async Task Withdrawal_PreservesOriginalSignedEvidence()
    {
        var draft = await _service.CreateDraftAsync(new CreateConsentDraftRequest
        {
            ChildId = _childId,
            ConsentTemplateId = _templateId
        });
        await _service.AdminReviewAsync(new AdminReviewRequest { DocumentId = draft.Id, Approve = true });
        var (_, token, _) = await _service.SendInvitationAsync(draft.Id, "https://localhost:7003/");

        await _service.SignDocumentAsync(new SubmitSignatureRequest
        {
            RawToken = token!,
            TypedLegalName = "Sunita Patel",
            LegalIntentConfirmed = true
        });

        // Withdraw consent
        var withdrawn = await _service.WithdrawConsentAsync(draft.Id, "Parent revoked photo permission.");
        Assert.That(withdrawn.Status, Is.EqualTo(ConsentWorkflowStatus.Withdrawn));
        Assert.That(withdrawn.SignedDocumentChecksum, Is.Not.Null); // Original checksum preserved
        Assert.That(_consentRepo.AuditEvents.Any(a => a.EventType == "Withdrawn"), Is.True);
    }
}
