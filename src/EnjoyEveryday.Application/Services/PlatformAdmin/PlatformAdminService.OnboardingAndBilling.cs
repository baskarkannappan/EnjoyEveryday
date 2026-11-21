using System.Collections.Concurrent;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public partial class PlatformAdminService
{
    #region Commercial & Billing Operations

    public Task<List<Invoice>> GetInvoicesAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _invoices.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            if (Enum.TryParse<InvoiceStatus>(status, true, out var parsedInvoiceStatus))
            {
                query = query.Where(i => i.Status == parsedInvoiceStatus);
            }
            else if (Enum.TryParse<FinancialStatus>(status, true, out var parsedFinancialStatus))
            {
                query = query.Where(i => i.FinancialStatus == parsedFinancialStatus);
            }
        }
        return Task.FromResult(query.OrderByDescending(i => i.DueDate).ToList());
    }

    public Task<List<Payment>> GetPaymentsAsync(bool? awaitingVerificationOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _payments.Values.AsEnumerable();
        if (awaitingVerificationOnly == true)
        {
            query = query.Where(p => !p.IsVerified);
        }
        return Task.FromResult(query.OrderByDescending(p => p.PaymentDate).ToList());
    }

    public Task<bool> VerifyPaymentWorkflowAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        if (_payments.TryGetValue(paymentId, out var payment))
        {
            payment.IsVerified = true;
            payment.VerifiedBy = _userContext.UserId != Guid.Empty ? _userContext.UserId.ToString() : "Platform Admin";
            payment.VerifiedAt = DateTimeOffset.UtcNow;

            if (payment.InvoiceId.HasValue && _invoices.TryGetValue(payment.InvoiceId.Value, out var invoice))
            {
                invoice.AmountPaid += payment.Amount;
                if (invoice.BalanceDue <= 0)
                {
                    invoice.Status = InvoiceStatus.Paid;
                    invoice.PaidAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    invoice.Status = InvoiceStatus.PartiallyPaid;
                }

                // Update organization financial status
                if (_organizations.TryGetValue(payment.OrganizationId, out var org))
                {
                    org.FinancialStatus = invoice.FinancialStatus;
                }
            }

            AddAuditLog("Payment Verified", $"Payment {payment.PaymentReference} of {payment.Amount:C0} reconciled and verified.", "Info", paymentId);

            if (_emailNotificationService != null && _organizations.TryGetValue(payment.OrganizationId, out var paymentOrg) && !string.IsNullOrWhiteSpace(paymentOrg.AdminEmail))
            {
                var daycareUrl = GetDaycarePortalUrl();
                _ = _emailNotificationService.SendPaymentReceiptConfirmationAsync(
                    paymentOrg.AdminEmail,
                    paymentOrg.Name,
                    payment.PaymentReference,
                    payment.Amount,
                    payment.Currency,
                    payment.PaymentMethod.ToString(),
                    $"{daycareUrl.TrimEnd('/')}/billing",
                    paymentOrg.TenantId,
                    cancellationToken);
            }

            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<Invoice> IssueInvoiceWorkflowAsync(Guid organizationId, decimal amount, string description, CancellationToken cancellationToken = default)
    {
        _organizations.TryGetValue(organizationId, out var org);
        var orgName = org?.Name ?? "Organization";

        var id = Guid.NewGuid();
        var invoice = new Invoice
        {
            Id = id,
            TenantId = org?.TenantId ?? organizationId,
            OrganizationId = organizationId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{new Random().Next(1000, 9999)}",
            Subtotal = amount,
            DiscountAmount = 0,
            TotalAmount = amount,
            AmountPaid = 0,
            Currency = "INR",
            Status = InvoiceStatus.Unpaid,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(14),
            Description = description,
            BillingContactEmail = org?.AdminEmail ?? "billing@daycare.com",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _invoices[id] = invoice;

        if (org != null)
        {
            org.FinancialStatus = FinancialStatus.InvoiceDue;
            org.LifecycleStageKey = "Invoiced";
        }

        AddAuditLog("Invoice Issued", $"Invoice {invoice.InvoiceNumber} for {amount:C0} issued to '{orgName}'.", "Info", id);

        if (_emailNotificationService != null && org != null && !string.IsNullOrWhiteSpace(org.AdminEmail))
        {
            var daycareUrl = GetDaycarePortalUrl();
            _ = _emailNotificationService.SendPlatformInvoiceNoticeAsync(
                org.AdminEmail,
                orgName,
                invoice.InvoiceNumber,
                invoice.TotalAmount,
                invoice.Currency,
                invoice.DueDate.DateTime,
                description,
                $"{daycareUrl.TrimEnd('/')}/billing",
                org.TenantId,
                cancellationToken);
        }

        return Task.FromResult(invoice);
    }

    public Task<List<ParentCommunication>> GetRecentParentCommunicationsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        var list = _communications.Values.OrderByDescending(c => c.CreatedAt).Take(limit).ToList();
        return Task.FromResult(list);
    }

    #endregion

    #region Organization Invitations & Onboarding Workflow

    public async Task<PlatformOrganizationInvitationRecord?> SendOrganizationInvitationWorkflowAsync(
        Guid organizationId, 
        string? customAdminEmail = null, 
        CancellationToken cancellationToken = default)
    {
        _organizations.TryGetValue(organizationId, out var org);
        var email = !string.IsNullOrWhiteSpace(customAdminEmail) ? customAdminEmail.Trim() : org?.AdminEmail;
        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Cannot send organization invitation: missing email for org {OrgId}", organizationId);
            return null;
        }

        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")[..16];
        PlatformOrganizationInvitationRecord? record = null;

        if (_platformAdminRepository != null)
        {
            try
            {
                record = await _platformAdminRepository.CreateOrganizationInvitationAsync(organizationId, email, token, 7, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not persist organization invitation to database; falling back to memory.");
            }
        }

        record ??= new PlatformOrganizationInvitationRecord
        {
            OrganizationId = organizationId,
            AdminEmail = email,
            Token = token,
            Status = "Pending",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var daycareUrl = GetDaycarePortalUrl();
        var activationUrl = $"{daycareUrl}/auth/activate?token={token}";
        var orgName = org?.Name ?? "Daycare Organization";
        var planName = org?.PlanName ?? "Standard Plan";

        if (_emailNotificationService != null)
        {
            try
            {
                await _emailNotificationService.SendOrgAdminInvitationAsync(
                    email,
                    orgName,
                    orgName,
                    planName,
                    activationUrl,
                    tempPassword: null,
                    org?.TenantId ?? organizationId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch onboarding invitation email to {Email} for {OrgName}", email, orgName);
            }
        }

        AddAuditLog("Admin Invitation Dispatched", $"Sent onboarding activation email to '{email}' for '{orgName}'. Token valid for 7 days.", "Info", organizationId);
        return record;
    }

    public async Task<Dictionary<Guid, PlatformOrganizationInvitationRecord>> GetLatestInvitationsMapAsync(CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            try
            {
                var list = await _platformAdminRepository.GetLatestInvitationsAsync(cancellationToken);
                return list.ToDictionary(k => k.OrganizationId, v => v);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to query latest organization invitations from repository.");
            }
        }
        return new Dictionary<Guid, PlatformOrganizationInvitationRecord>();
    }

    public async Task<PlatformOrganizationInvitationValidationResult?> ValidateInvitationAsync(string token, CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            return await _platformAdminRepository.ValidateInvitationAsync(token, cancellationToken);
        }
        return null;
    }

    public async Task<bool> AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default)
    {
        if (_platformAdminRepository != null)
        {
            var accepted = await _platformAdminRepository.AcceptInvitationAsync(token, userId, cancellationToken);
            if (accepted)
            {
                AddAuditLog("Invitation Accepted", $"Organization activation invitation token successfully accepted by User {userId}.", "Info");
            }
            return accepted;
        }
        return false;
    }

    #endregion

    #region Plan & Tariff Subscriber Notifications

    public async Task<int> NotifySubscribersOfPlanUpdateAsync(Guid planId, string updateTitle, string changesSummaryHtml, CancellationToken cancellationToken = default)
    {
        _plans.TryGetValue(planId, out var plan);
        var planName = plan?.Name ?? "Subscription Plan";

        var subscribers = _organizations.Values
            .Where(o => o.PlanId == planId && (o.AccountStatus == AccountStatus.Active || o.AccountStatus == AccountStatus.Trial))
            .ToList();

        var daycareUrl = GetDaycarePortalUrl();
        var billingUrl = $"{daycareUrl}/billing";
        var count = 0;

        foreach (var sub in subscribers)
        {
            if (string.IsNullOrWhiteSpace(sub.AdminEmail)) continue;

            if (_emailNotificationService != null)
            {
                try
                {
                    await _emailNotificationService.SendPlanOrTariffUpdateNoticeAsync(
                        sub.AdminEmail,
                        sub.Name,
                        planName,
                        updateTitle,
                        changesSummaryHtml,
                        "Next billing cycle",
                        billingUrl,
                        sub.TenantId,
                        cancellationToken);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify org {OrgId} of plan update", sub.Id);
                }
            }
        }

        AddAuditLog("Plan Subscribers Notified", $"Dispatched plan update notice '{updateTitle}' to {count} active subscribers of '{planName}'.", "Info", planId);
        return count;
    }

    public async Task<int> NotifySubscribersOfTariffUpdateAsync(Guid tariffId, string updateTitle, string changesSummaryHtml, CancellationToken cancellationToken = default)
    {
        _tariffs.TryGetValue(tariffId, out var tariff);
        var tariffName = tariff?.Name ?? "Tariff";

        var subscribers = _organizations.Values
            .Where(o => o.TariffId == tariffId && (o.AccountStatus == AccountStatus.Active || o.AccountStatus == AccountStatus.Trial))
            .ToList();

        var daycareUrl = GetDaycarePortalUrl();
        var billingUrl = $"{daycareUrl}/billing";
        var count = 0;

        foreach (var sub in subscribers)
        {
            if (string.IsNullOrWhiteSpace(sub.AdminEmail)) continue;

            if (_emailNotificationService != null)
            {
                try
                {
                    await _emailNotificationService.SendPlanOrTariffUpdateNoticeAsync(
                        sub.AdminEmail,
                        sub.Name,
                        tariffName,
                        updateTitle,
                        changesSummaryHtml,
                        "Next billing cycle",
                        billingUrl,
                        sub.TenantId,
                        cancellationToken);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify org {OrgId} of tariff update", sub.Id);
                }
            }
        }

        AddAuditLog("Tariff Subscribers Notified", $"Dispatched tariff update notice '{updateTitle}' to {count} active subscribers of '{tariffName}'.", "Info", tariffId);
        return count;
    }

    #endregion

    #region Email Auditing

    public async Task<List<EmailNotificationLogDto>> GetEmailAuditLogsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        if (_emailNotificationService != null)
        {
            return await _emailNotificationService.GetRecentEmailLogsAsync(limit, cancellationToken);
        }
        return new List<EmailNotificationLogDto>();
    }

    #endregion

    private string GetDaycarePortalUrl()
    {
        var url = _configuration?["DaycareUrl"]
               ?? _configuration?["Services:DaycareUrl"]
               ?? _configuration?["DaycarePortalUrl"]
               ?? _configuration?["Urls:Daycare"]
               ?? "https://localhost:7003";

        if (url.StartsWith("http://localhost:7003", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://localhost:7003" + url["http://localhost:7003".Length..];
        }

        return url.TrimEnd('/');
    }

    private static void AddAuditLog(string action, string details, string severity, Guid? entityId = null)
    {
        lock (_lock)
        {
            _auditLogs.Insert(0, new PlatformAuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                Actor = "Platform Superadmin",
                Action = action,
                EntityType = "Platform",
                EntityId = entityId,
                Details = details,
                Severity = severity
            });
            if (_auditLogs.Count > 100)
            {
                _auditLogs.RemoveAt(_auditLogs.Count - 1);
            }
        }
    }

    private static void InitializeSeedData()
    {
        // Plans
        var starterPlan = new SubscriptionPlan
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Sprout (Starter)",
            Code = "PLAN-SPROUT",
            Description = "Ideal for single-branch nurseries and boutique preschools.",
            BasePrice = 149.00m,
            Frequency = BillingFrequency.Monthly,
            MaxBranches = 1,
            MaxClassrooms = 3,
            MaxChildren = 35,
            IncludesAiFeatures = false
        };

        var growthPlan = new SubscriptionPlan
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Bloom (Growth)",
            Code = "PLAN-BLOOM",
            Description = "Comprehensive suite with AI assistants and multiple classrooms.",
            BasePrice = 299.00m,
            Frequency = BillingFrequency.Monthly,
            MaxBranches = 2,
            MaxClassrooms = 8,
            MaxChildren = 90,
            IncludesAiFeatures = true
        };

        var canopyPlan = new SubscriptionPlan
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Canopy (Multi-Branch)",
            Code = "PLAN-CANOPY",
            Description = "Full multi-branch daycare network with organization intelligence.",
            BasePrice = 599.00m,
            Frequency = BillingFrequency.Monthly,
            MaxBranches = 6,
            MaxClassrooms = 24,
            MaxChildren = 300,
            IncludesAiFeatures = true
        };

        _plans[starterPlan.Id] = starterPlan;
        _plans[growthPlan.Id] = growthPlan;
        _plans[canopyPlan.Id] = canopyPlan;

        // Tariffs
        var t1 = new Tariff
        {
            Id = Guid.NewGuid(),
            PlanId = starterPlan.Id,
            Name = "Starter Monthly",
            Frequency = BillingFrequency.Monthly,
            Price = 149.00m,
            DiscountPercentage = 0,
            IsActive = true
        };
        var t2 = new Tariff
        {
            Id = Guid.NewGuid(),
            PlanId = starterPlan.Id,
            Name = "Starter Annual (Save 15%)",
            Frequency = BillingFrequency.Annual,
            Price = 126.00m,
            DiscountPercentage = 15,
            IsActive = true
        };

        var t3 = new Tariff
        {
            Id = Guid.NewGuid(),
            PlanId = growthPlan.Id,
            Name = "Growth Monthly",
            Frequency = BillingFrequency.Monthly,
            Price = 299.00m,
            DiscountPercentage = 0,
            IsActive = true
        };
        var t4 = new Tariff
        {
            Id = Guid.NewGuid(),
            PlanId = growthPlan.Id,
            Name = "Growth Annual (Save 20%)",
            Frequency = BillingFrequency.Annual,
            Price = 239.00m,
            DiscountPercentage = 20,
            IsActive = true
        };

        var t5 = new Tariff
        {
            Id = Guid.NewGuid(),
            PlanId = canopyPlan.Id,
            Name = "Canopy Monthly",
            Frequency = BillingFrequency.Monthly,
            Price = 599.00m,
            DiscountPercentage = 0,
            IsActive = true
        };

        _tariffs[t1.Id] = t1;
        _tariffs[t2.Id] = t2;
        _tariffs[t3.Id] = t3;
        _tariffs[t4.Id] = t4;
        _tariffs[t5.Id] = t5;

        starterPlan.Tariffs.AddRange(new[] { t1, t2 });
        growthPlan.Tariffs.AddRange(new[] { t3, t4 });
        canopyPlan.Tariffs.Add(t5);

        // Seed Organizations matching database seeds (002_seed_data.sql)
        var org1Id = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var org1Tenant = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var org1 = new PlatformOrganizationDto
        {
            Id = org1Id,
            TenantId = org1Tenant,
            Name = "Little Stars Daycare",
            Location = "Toronto · 3 branches",
            BranchCount = 3,
            AccountStatus = AccountStatus.Active,
            FinancialStatus = FinancialStatus.Reconciled,
            PlanId = growthPlan.Id,
            PlanName = "Bloom (Growth)",
            TariffId = t3.Id,
            TariffName = "Growth Monthly",
            MonthlyRate = 299.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(18),
            AdminEmail = "admin@littlestars.com",
            LifecycleStageKey = "Active",
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6)
        };

        var org2Id = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var org2Tenant = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var org2 = new PlatformOrganizationDto
        {
            Id = org2Id,
            TenantId = org2Tenant,
            Name = "Sunshine Garden Preschool",
            Location = "Toronto · 2 branches",
            BranchCount = 2,
            AccountStatus = AccountStatus.Active,
            FinancialStatus = FinancialStatus.Reconciled,
            PlanId = starterPlan.Id,
            PlanName = "Sprout (Starter)",
            TariffId = t1.Id,
            TariffName = "Starter Monthly",
            MonthlyRate = 149.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(22),
            AdminEmail = "daniel@sunshinegarden.com",
            LifecycleStageKey = "Active",
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-3)
        };

        var org3Id = Guid.Parse("20000000-0000-0000-0000-000000000003");
        var org3Tenant = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var org3 = new PlatformOrganizationDto
        {
            Id = org3Id,
            TenantId = org3Tenant,
            Name = "Happy Roots Early Learning",
            Location = "Vancouver · 1 branch",
            BranchCount = 1,
            AccountStatus = AccountStatus.Active,
            FinancialStatus = FinancialStatus.InvoiceDue,
            PlanId = canopyPlan.Id,
            PlanName = "Canopy (Multi-Branch)",
            TariffId = t5.Id,
            TariffName = "Canopy Monthly",
            MonthlyRate = 599.00m,
            NextBillingDate = DateTimeOffset.UtcNow.AddDays(5),
            AdminEmail = "director@happyroots.com",
            LifecycleStageKey = "Invoiced",
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-11)
        };

        _organizations[org1Id] = org1;
        _organizations[org2Id] = org2;
        _organizations[org3Id] = org3;

        // Invoices
        var inv1Id = Guid.NewGuid();
        _invoices[inv1Id] = new Invoice
        {
            Id = inv1Id,
            TenantId = org3Tenant,
            OrganizationId = org3Id,
            InvoiceNumber = "INV-202610-8491",
            Subtotal = 599.00m,
            TotalAmount = 599.00m,
            AmountPaid = 0,
            Status = InvoiceStatus.Unpaid,
            IssueDate = DateTimeOffset.UtcNow.AddDays(-10),
            DueDate = DateTimeOffset.UtcNow.AddDays(5),
            Description = "Canopy Tier Subscription - October 2026",
            BillingContactEmail = "director@happyroots.com"
        };

        var inv2Id = Guid.NewGuid();
        _invoices[inv2Id] = new Invoice
        {
            Id = inv2Id,
            TenantId = org2Tenant,
            OrganizationId = org2Id,
            InvoiceNumber = "INV-202609-3120",
            Subtotal = 149.00m,
            TotalAmount = 149.00m,
            AmountPaid = 0,
            Status = InvoiceStatus.Overdue,
            IssueDate = DateTimeOffset.UtcNow.AddDays(-38),
            DueDate = DateTimeOffset.UtcNow.AddDays(-8),
            Description = "Sprout Tier Subscription - September 2026",
            BillingContactEmail = "daniel@sunshinegarden.com"
        };

        // Payments
        var pay1Id = Guid.NewGuid();
        _payments[pay1Id] = new Payment
        {
            Id = pay1Id,
            TenantId = org1Tenant,
            OrganizationId = org1Id,
            PaymentReference = "PAY-ACH-99410",
            Amount = 299.00m,
            Method = "ACH",
            IsVerified = false,
            PaymentDate = DateTimeOffset.UtcNow.AddDays(-1),
            Status = "Pending Verification",
            TransactionId = "txn_ach_88319041"
        };

        // Seed AI Parent Communications (Grounded, Teacher-Approved)
        var comm1Id = Guid.NewGuid();
        _communications[comm1Id] = new ParentCommunication
        {
            Id = comm1Id,
            TenantId = org1Id,
            OrganizationId = org1Id,
            ChildId = Guid.NewGuid(),
            ChildName = "Aarav Patel",
            ClassroomName = "Bluebirds",
            RecipientName = "Sunita Patel",
            RecipientRelationship = "Mother",
            RecipientEmail = "sunita.patel@test.com",
            CommunicationType = ParentCommunicationTypes.DailyUpdate,
            Title = "Aarav's Day in Bluebirds — Oct 14",
            ContentDraft = "Dear Sunita,\n\nAarav had a wonderful morning of cooperative play. He gently observed a ladybug during our outdoor nature walk and helped his friends build a bridge with wooden blocks.\n\nWarmly,\nRachel",
            ContentFinal = "Dear Sunita,\n\nAarav had a wonderful morning of cooperative play. He gently observed a ladybug during our outdoor nature walk and helped his friends build a bridge with wooden blocks.\n\nWarmly,\nRachel",
            FamilyConversationStarter = "Ask Aarav how the ladybug crawled across the leaf today!",
            Status = ParentCommunicationStatus.Sent,
            TeacherName = "Rachel Green",
            ConsentVerified = true,
            ApprovedAt = DateTimeOffset.UtcNow.AddHours(-3),
            SentAt = DateTimeOffset.UtcNow.AddHours(-3),
            DeliveryChannel = "PortalAndEmail",
            DeliveryStatus = "Delivered",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-4)
        };

        var comm2Id = Guid.NewGuid();
        _communications[comm2Id] = new ParentCommunication
        {
            Id = comm2Id,
            TenantId = org2Id,
            OrganizationId = org2Id,
            ChildId = Guid.NewGuid(),
            ChildName = "Ella Lindqvist",
            ClassroomName = "Pinecones",
            RecipientName = "Karin Lindqvist",
            RecipientRelationship = "Mother",
            RecipientEmail = "karin@nordicpine.edu",
            CommunicationType = ParentCommunicationTypes.WeeklySummary,
            Title = "Ella's Weekly Learning Journey in Pinecones",
            ContentDraft = "Dear Karin,\n\nElla showed great focus this week exploring textures and autumn leaves in our sensory tray. She enjoyed singing rhythmic songs with her peers.\n\nWarmly,\nTeacher Sarah",
            ContentFinal = "Dear Karin,\n\nElla showed great focus this week exploring textures and autumn leaves in our sensory tray. She enjoyed singing rhythmic songs with her peers.\n\nWarmly,\nTeacher Sarah",
            FamilyConversationStarter = "Ask Ella about the crunchy brown leaf she collected yesterday!",
            Status = ParentCommunicationStatus.PendingReview,
            TeacherName = "Sarah Connor",
            ConsentVerified = true,
            ApprovedAt = null,
            SentAt = null,
            DeliveryChannel = "PortalOnly",
            DeliveryStatus = "Pending",
            CreatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        // Audit Logs
        AddAuditLog("System Boot", "EnjoyEveryday Platform Administration services initialized.", "Info");
        AddAuditLog("Organization Created", "Sunshine Garden Preschool initiated registration.", "Info", org2Id);
        AddAuditLog("Payment Submitted", "ACH payment PAY-ACH-99410 for $299.00 awaiting administrative reconciliation.", "Warning", pay1Id);
        AddAuditLog("AI Parent Communication Sent", "Daily update draft approved by Teacher Rachel Green and dispatched to parent Sunita Patel.", "Info", comm1Id);
    }
}
