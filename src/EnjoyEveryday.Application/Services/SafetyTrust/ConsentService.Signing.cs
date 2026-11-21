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


public partial class ConsentService
{
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
            Choices = choices,
            ActiveRawToken = rawToken
        };
    }

    public async Task<ParentSigningSession> GetSigningSessionForParentAsync(Guid documentId, CancellationToken ct = default)
    {
        var doc = await _repository.GetDocumentByIdAsync(_tenantContext.TenantId, documentId, ct);
        if (doc == null)
        {
            return new ParentSigningSession { IsInvalid = true, ErrorMessage = "Associated consent document not found." };
        }

        // Verify parent authorization for this child
        if (_userContext?.UserId != null && _userContext.UserId != Guid.Empty)
        {
            var authorizedChildren = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, ct);
            if (!authorizedChildren.Any(c => c.ChildId == doc.ChildId))
            {
                return new ParentSigningSession { IsInvalid = true, ErrorMessage = "You are not authorized to review or sign documents for this child." };
            }
        }

        var signer = doc.Signers.FirstOrDefault(s => !string.IsNullOrEmpty(_userContext?.Email) && s.SignerEmailSnapshot.Equals(_userContext.Email, StringComparison.OrdinalIgnoreCase))
                     ?? doc.Signers.FirstOrDefault();

        if (signer == null)
        {
            return new ParentSigningSession { IsInvalid = true, ErrorMessage = "Signer profile not configured for this consent document." };
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

        // Generate or refresh active token for this authenticated parent session
        var rawToken = GenerateRawToken();
        signer.TokenHash = HashToken(rawToken);
        signer.TokenExpiresAt = doc.SigningDeadline ?? DateTime.UtcNow.AddDays(7);
        if (signer.SigningStatus != "Signed")
        {
            signer.SigningStatus = "Opened";
            signer.LastOpenedAt = DateTime.UtcNow;
        }
        await _repository.UpdateSignerAsync(signer, ct);

        if (doc.Status == ConsentWorkflowStatus.SentToParent)
        {
            doc.Status = ConsentWorkflowStatus.Opened;
            await _repository.UpdateDocumentAsync(doc, ct);
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
            Choices = choices,
            ActiveRawToken = rawToken
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

        // Send parent confirmation email with mandatory Org Admin CC
        var orgAdminCc = _eventService != null && doc.OrganizationId.HasValue
            ? await _eventService.ResolveOrganizationAdminEmailAsync(doc.OrganizationId.Value, ct)
            : null;

        _ = _emailService.SendSigningConfirmationAsync(
            signer.SignerEmailSnapshot,
            signer.SignatureLegalName,
            doc.ChildName?.Split(' ').FirstOrDefault() ?? "Your Child",
            doc.DocumentTitle,
            doc.SignedAt.Value,
            orgAdminCc,
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
        if (!_userContext.HasPermission("consent.manage"))
            throw new UnauthorizedAccessException("Requires consent.manage permission.");

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
