using EnjoyEveryday.Domain.Constants;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;


public partial class ParentService
{
    public async Task<IEnumerable<ParentChatMessage>> GetChildMessagesAsync(Guid childId, int take = 50, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);

        var messages = new List<ParentChatMessage>();

        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId);
        var effectiveTenantId = (child != null && child.TenantId != Guid.Empty) ? child.TenantId : _tenantContext.TenantId;

        if (_communicationRepository != null)
        {
            var comms = await _communicationRepository.GetByChildIdAsync(effectiveTenantId, childId, cancellationToken);
            foreach (var c in comms)
            {
                var isFromParent = (c.TeacherId == Guid.Empty);
                var content = !string.IsNullOrWhiteSpace(c.ContentFinal) ? c.ContentFinal : c.ContentDraft;

                if (!string.IsNullOrWhiteSpace(content))
                {
                    messages.Add(new ParentChatMessage
                    {
                        Id = c.Id,
                        ChildId = c.ChildId,
                        SenderName = isFromParent ? "You" : (!string.IsNullOrWhiteSpace(c.TeacherName) ? c.TeacherName : $"{child?.ClassroomName ?? "Classroom"} Educators"),
                        Content = content,
                        Timestamp = c.SentAt ?? c.CreatedAt,
                        IsFromParent = isFromParent,
                        DeliveryStatus = c.DeliveryStatus ?? "Delivered"
                    });
                }
            }
        }

        // If no prior message history, seed an initial friendly educator check-in
        if (messages.Count == 0)
        {
            var childName = child?.FirstName ?? "Your child";
            var classroomName = child?.ClassroomName ?? "Classroom";

            messages.Add(new ParentChatMessage
            {
                Id = Guid.NewGuid(),
                ChildId = childId,
                SenderName = $"{classroomName} Educators",
                Content = $"Hello! Welcome to the {classroomName} communication channel. Feel free to message us here about {childName}'s day or questions.",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-3),
                IsFromParent = false,
                DeliveryStatus = "Read"
            });
        }

        return messages.OrderBy(m => m.Timestamp).TakeLast(take);
    }

    public async Task<ParentChatMessage> SendParentMessageAsync(Guid childId, string messageText, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);

        var sanitized = messageText?.Trim();
        if (string.IsNullOrWhiteSpace(sanitized))
            throw new ArgumentException("Message cannot be empty.", nameof(messageText));

        if (sanitized.Length > 2000)
            sanitized = sanitized.Substring(0, 2000);

        var person = await _parentRepository.GetPersonByUserIdAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var parentName = person != null ? $"{person.FirstName} {person.LastName}".Trim() : "Parent";

        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId)
            ?? throw new InvalidOperationException("Child not found.");

        var messageId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        if (_communicationRepository != null)
        {
            var comm = new ParentCommunication
            {
                Id = messageId,
                TenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId,
                OrganizationId = child.OrganizationId ?? Guid.Empty,
                ClassroomId = child.ClassroomId,
                ChildId = childId,
                RecipientUserId = _userContext.UserId,
                RecipientPersonId = person?.Id,
                RecipientName = parentName,
                RecipientRelationship = child.Relationship,
                CommunicationType = "ParentMessage",
                ReportingDateStart = DateOnly.FromDateTime(DateTime.Today),
                ReportingDateEnd = DateOnly.FromDateTime(DateTime.Today),
                Title = $"Message from {parentName}",
                ContentDraft = sanitized,
                ContentFinal = sanitized,
                Status = ParentCommunicationStatus.Sent,
                TeacherId = Guid.Empty,
                TeacherName = $"{child.ClassroomName ?? "Classroom"} Educators",
                SentAt = now,
                DeliveryChannel = "Portal",
                DeliveryStatus = "Delivered",
                CreatedAt = now,
                UpdatedAt = now
            };

            await _communicationRepository.CreateAsync(comm, cancellationToken);
        }

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "ParentMessageSent",
            EntityType = "ParentCommunication",
            EntityId = messageId,
            Details = new { ChildId = childId, TextLength = sanitized.Length }
        }, cancellationToken);

        if (_realtimeNotificationService != null)
        {
            _ = _realtimeNotificationService.NotifyParentMessageSentAsync(new ParentMessageNotification
            {
                MessageId = messageId,
                TenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId,
                OrganizationId = child.OrganizationId ?? Guid.Empty,
                BranchId = child.BranchId,
                ClassroomId = child.ClassroomId,
                ChildId = childId,
                ChildName = $"{child.FirstName} {child.LastName}".Trim(),
                ClassroomName = child.ClassroomName ?? "Classroom",
                SenderName = parentName,
                Content = sanitized,
                Timestamp = now,
                IsFromParent = true
            }, cancellationToken);
        }

        return new ParentChatMessage
        {
            Id = messageId,
            ChildId = childId,
            SenderName = "You",
            Content = sanitized,
            Timestamp = now,
            IsFromParent = true,
            DeliveryStatus = "Delivered"
        };
    }

    public async Task ReportAbsenceAsync(Guid childId, DateTime startDate, DateTime endDate, string reason, string? notes, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);

        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId)
            ?? throw new InvalidOperationException("Child not found.");

        var person = await _parentRepository.GetPersonByUserIdAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var parentName = person != null ? $"{person.FirstName} {person.LastName}".Trim() : (_userContext.Email ?? "Parent");

        var messageId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var absenceDetails = $"Absence Notice: {startDate:d MMM yyyy} to {endDate:d MMM yyyy}. Reason: {reason}." + (!string.IsNullOrWhiteSpace(notes) ? $" Notes: {notes}" : "");

        if (_communicationRepository != null)
        {
            var comm = new ParentCommunication
            {
                Id = messageId,
                TenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId,
                OrganizationId = child.OrganizationId ?? Guid.Empty,
                ClassroomId = child.ClassroomId,
                ChildId = childId,
                RecipientUserId = _userContext.UserId,
                RecipientPersonId = person?.Id,
                RecipientName = parentName,
                RecipientRelationship = child.Relationship,
                CommunicationType = "AbsenceNotice",
                ReportingDateStart = DateOnly.FromDateTime(startDate),
                ReportingDateEnd = DateOnly.FromDateTime(endDate),
                Title = $"Absence reported for {child.FirstName}",
                ContentDraft = absenceDetails,
                ContentFinal = absenceDetails,
                Status = ParentCommunicationStatus.PendingReview,
                TeacherId = Guid.Empty,
                TeacherName = $"{child.ClassroomName ?? "Classroom"} Educators",
                SentAt = now,
                DeliveryChannel = "Portal",
                DeliveryStatus = "Delivered",
                CreatedAt = now,
                UpdatedAt = now
            };

            await _communicationRepository.CreateAsync(comm, cancellationToken);
        }

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "ParentReportedAbsence",
            EntityType = "ChildAttendanceAbsenceNotice",
            EntityId = messageId,
            Details = new { ChildId = childId, StartDate = startDate, EndDate = endDate, Reason = reason }
        }, cancellationToken);
    }

    public async Task<ChildPickupSecurityProfile?> GetChildPickupSecurityProfileAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        if (_pickupService == null) return null;
        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId);
        var effectiveTenantId = child?.TenantId != null && child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId;
        return await _pickupService.GetChildPickupSecurityProfileAsync(effectiveTenantId, childId, null, cancellationToken);
    }

    public async Task<List<Invoice>> GetChildInvoicesAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        if (_orgFeeRepository != null)
        {
            var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
            var child = children.FirstOrDefault(c => c.ChildId == childId);
            var effectiveTenantId = child?.TenantId != null && child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId;
            return await _orgFeeRepository.GetChildInvoicesAsync(effectiveTenantId, childId, cancellationToken);
        }
        if (_billingService == null) return new List<Invoice>();
        var allInvoices = await _billingService.GetAllInvoicesAsync(ct: cancellationToken);
        return allInvoices.Where(i => i.ChildId == childId).ToList();
    }

    public async Task<ParentPaymentOrderDto> CreateInvoicePaymentOrderAsync(Guid childId, Guid invoiceId, CancellationToken ct = default)
    {
        await ValidateAccessOrThrow(childId, ct);
        if (_billingService == null) throw new InvalidOperationException("Billing service is currently unavailable.");
        if (_razorpayGateway == null) throw new InvalidOperationException("Payment gateway is currently unavailable.");

        var invoice = await _billingService.GetInvoiceByIdAsync(invoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

        if (invoice.BalanceDue <= 0 || invoice.Status == InvoiceStatus.Paid)
        {
            throw new InvalidOperationException("This invoice has already been fully paid.");
        }

        long amountInPaise = (long)Math.Round(invoice.BalanceDue * 100, MidpointRounding.AwayFromZero);
        var targetCurrency = string.IsNullOrWhiteSpace(invoice.Currency) ? "INR" : invoice.Currency.ToUpperInvariant();

        var notes = new Dictionary<string, string>
        {
            ["invoice_id"] = invoice.Id.ToString(),
            ["child_id"] = childId.ToString(),
            ["tenant_id"] = invoice.TenantId.ToString()
        };

        var gatewayResult = await _razorpayGateway.CreateOrderAsync(amountInPaise, targetCurrency, invoice.InvoiceNumber, notes, ct);

        return new ParentPaymentOrderDto
        {
            KeyId = _razorpayGateway.Settings.KeyId,
            OrderId = gatewayResult.OrderId,
            Amount = invoice.BalanceDue,
            AmountInPaise = amountInPaise,
            Currency = targetCurrency,
            InvoiceNumber = invoice.InvoiceNumber,
            OrganizationName = "EnjoyEveryday Daycare",
            PrefillName = !string.IsNullOrWhiteSpace(_userContext.Email) ? _userContext.Email : "Parent Guardian",
            PrefillEmail = _userContext.Email ?? "",
            PrefillContact = ""
        };
    }

    public async Task<ParentPaymentResultDto> VerifyAndApplyPaymentAsync(Guid childId, Guid invoiceId, string orderId, string paymentId, string signature, CancellationToken ct = default)
    {
        await ValidateAccessOrThrow(childId, ct);
        if (_billingService == null) throw new InvalidOperationException("Billing service is currently unavailable.");
        if (_razorpayGateway == null) throw new InvalidOperationException("Payment gateway is currently unavailable.");

        var invoice = await _billingService.GetInvoiceByIdAsync(invoiceId, ct)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} was not found.");

        // 1. Verify cryptographic signature
        var isValid = _razorpayGateway.VerifyPaymentSignature(orderId, paymentId, signature);
        if (!isValid)
        {
            return new ParentPaymentResultDto
            {
                Success = false,
                ErrorMessage = "Payment signature could not be verified by the server. Transaction was not confirmed."
            };
        }

        // 2. Apply payment
        var paymentAmount = invoice.BalanceDue;
        var verifyRequest = new VerifyRazorpayPaymentRequest
        {
            InvoiceId = invoiceId,
            RazorpayOrderId = orderId,
            RazorpayPaymentId = paymentId,
            RazorpaySignature = signature
        };

        var verifyResult = await _billingService.VerifyRazorpayPaymentAsync(verifyRequest, ct);
        if (!verifyResult.Success)
        {
            return new ParentPaymentResultDto
            {
                Success = false,
                ErrorMessage = verifyResult.ErrorMessage ?? "Payment verification failed."
            };
        }

        var updatedInvoice = verifyResult.Invoice ?? await _billingService.GetInvoiceByIdAsync(invoiceId, ct);

        return new ParentPaymentResultDto
        {
            Success = true,
            PaymentId = paymentId,
            AmountPaid = paymentAmount,
            NewBalanceDue = updatedInvoice?.BalanceDue ?? 0,
            NewStatus = updatedInvoice?.Status.ToString() ?? "Paid"
        };
    }

    public async Task<AuthorizedPickupDto> SaveAuthorizedPickupAsync(Guid childId, AuthorizedPickupDto dto, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        if (_pickupService == null)
        {
            throw new InvalidOperationException("Pickup service is not available.");
        }

        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId)
            ?? throw new InvalidOperationException("Child not found.");
        var effectiveTenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId;

        return await _pickupService.SaveAuthorizedPickupAsync(effectiveTenantId, childId, dto, cancellationToken);
    }

    public async Task<bool> DeleteAuthorizedPickupAsync(Guid childId, Guid pickupId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        if (_pickupService == null) return false;

        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId)
            ?? throw new InvalidOperationException("Child not found.");
        var effectiveTenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId;

        return await _pickupService.DeleteAuthorizedPickupAsync(effectiveTenantId, childId, pickupId, cancellationToken);
    }

    public async Task<bool> IsPrivacyAndTrustApprovedAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId);
        var effectiveTenantId = child != null && child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId;

        return await _parentRepository.IsPrivacyAndTrustApprovedAsync(effectiveTenantId, childId, cancellationToken);
    }

    public async Task<DateTimeOffset?> GetPrivacyAndTrustApprovalDateAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId);
        var effectiveTenantId = child != null && child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId;

        return await _parentRepository.GetPrivacyAndTrustApprovalDateAsync(effectiveTenantId, childId, cancellationToken);
    }

    public async Task ApprovePrivacyAndTrustAsync(Guid childId, string approvedByName, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId)
            ?? throw new InvalidOperationException("Child not found.");
        var effectiveTenantId = child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId;

        await _parentRepository.ApprovePrivacyAndTrustAsync(effectiveTenantId, childId, approvedByName, cancellationToken);

        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = effectiveTenantId,
            UserId = _userContext.UserId,
            Action = "PrivacyAndTrustApproved",
            EntityType = "ChildConsent",
            EntityId = childId,
            Details = new { ChildId = childId, ApprovedBy = approvedByName }
        }, cancellationToken);
    }

    public async Task MarkMessagesAsReadAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        await ValidateAccessOrThrow(childId, cancellationToken);
        if (_communicationRepository == null) return;

        var children = await _parentRepository.GetAuthorizedChildrenAsync(_tenantContext.TenantId, _userContext.UserId, cancellationToken);
        var child = children.FirstOrDefault(c => c.ChildId == childId);
        var effectiveTenantId = child != null && child.TenantId != Guid.Empty ? child.TenantId : _tenantContext.TenantId;

        await _communicationRepository.MarkAsReadByParentAsync(effectiveTenantId, childId, cancellationToken);
    }
}
