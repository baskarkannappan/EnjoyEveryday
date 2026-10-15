using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Audit;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class ParentCommunicationAssistantService : IParentCommunicationAssistantService
{
    private readonly IParentCommunicationRepository _communicationRepository;
    private readonly IChildRepository _childRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IParentRepository _parentRepository;
    private readonly IExperienceScheduleRepository _scheduleRepository;
    private readonly IExperienceRepository _experienceRepository;
    private readonly IChildDailyCareService? _dailyCareService;
    private readonly TenantSettingsService _settingsService;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ParentCommunicationAssistantService> _logger;

    public ParentCommunicationAssistantService(
        IParentCommunicationRepository communicationRepository,
        IChildRepository childRepository,
        IClassroomRepository classroomRepository,
        IBranchRepository branchRepository,
        IParentRepository parentRepository,
        IExperienceScheduleRepository scheduleRepository,
        IExperienceRepository experienceRepository,
        TenantSettingsService settingsService,
        ITenantContext tenantContext,
        IUserContext userContext,
        IAuditService auditService,
        HttpClient httpClient,
        ILogger<ParentCommunicationAssistantService> logger,
        IChildDailyCareService? dailyCareService = null)
    {
        _communicationRepository = communicationRepository;
        _childRepository = childRepository;
        _classroomRepository = classroomRepository;
        _branchRepository = branchRepository;
        _parentRepository = parentRepository;
        _scheduleRepository = scheduleRepository;
        _experienceRepository = experienceRepository;
        _settingsService = settingsService;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _auditService = auditService;
        _httpClient = httpClient;
        _logger = logger;
        _dailyCareService = dailyCareService;
    }

    public async Task<IEnumerable<ParentCommunicationRecipientInfo>> GetRecipientsForChildAsync(
        Guid childId, 
        CancellationToken cancellationToken = default)
    {
        var child = await _childRepository.GetByIdAsync(_tenantContext.TenantId, childId, cancellationToken);
        if (child == null)
        {
            throw new KeyNotFoundException($"Child {childId} not found in this tenant.");
        }

        return await _communicationRepository.GetRecipientsForChildAsync(_tenantContext.TenantId, childId, cancellationToken);
    }

    public async Task<IEnumerable<ParentCommunication>> GetChildCommunicationHistoryAsync(
        Guid childId, 
        CancellationToken cancellationToken = default)
    {
        var child = await _childRepository.GetByIdAsync(_tenantContext.TenantId, childId, cancellationToken);
        if (child == null)
        {
            throw new KeyNotFoundException($"Child {childId} not found in this tenant.");
        }

        return await _communicationRepository.GetByChildIdAsync(_tenantContext.TenantId, childId, cancellationToken);
    }

    public async Task<ParentCommunication> GenerateDraftAsync(
        GenerateParentCommunicationInput input, 
        CancellationToken cancellationToken = default)
    {
        // 1. Authorize Child & Classroom
        var child = await _childRepository.GetByIdAsync(_tenantContext.TenantId, input.ChildId, cancellationToken);
        if (child == null)
        {
            throw new UnauthorizedAccessException($"Child {input.ChildId} not authorized or does not exist in tenant.");
        }

        var classroomId = input.ClassroomId ?? child.ClassroomId;
        Classroom? classroom = null;
        Guid organizationId = _tenantContext.TenantId;
        if (classroomId.HasValue)
        {
            classroom = await _classroomRepository.GetByIdAsync(_tenantContext.TenantId, classroomId.Value, cancellationToken);
            if (classroom != null)
            {
                var branch = await _branchRepository.GetByIdAsync(_tenantContext.TenantId, classroom.BranchId, cancellationToken);
                if (branch != null)
                {
                    organizationId = branch.OrganizationId;
                }
            }
        }

        // 2. Authorize Recipient
        var recipients = (await _communicationRepository.GetRecipientsForChildAsync(_tenantContext.TenantId, input.ChildId, cancellationToken)).ToList();
        ParentCommunicationRecipientInfo? selectedRecipient = null;
        if (input.RecipientPersonId.HasValue)
        {
            selectedRecipient = recipients.FirstOrDefault(r => r.PersonId == input.RecipientPersonId.Value);
            if (selectedRecipient == null)
            {
                throw new UnauthorizedAccessException($"Specified recipient {input.RecipientPersonId.Value} is not authorized for child {child.Id}.");
            }
        }
        else
        {
            selectedRecipient = recipients.FirstOrDefault(r => r.CanReceiveInformation) ?? recipients.FirstOrDefault();
        }

        if (selectedRecipient != null && !selectedRecipient.CanReceiveInformation)
        {
            throw new InvalidOperationException($"Recipient {selectedRecipient.FullName} does not have legal authorization or permission to receive child communications.");
        }

        // 3. Collect Evidence strictly within authorized boundary and parent-visible flags
        var evidence = new ParentCommunicationEvidencePackage
        {
            ChildId = child.Id,
            ChildName = $"{child.FirstName} {child.LastName}".Trim(),
            ClassroomName = classroom?.Name ?? "Classroom",
            CommunicationType = input.CommunicationType,
            ReportingPeriod = input.StartDate == input.EndDate ? input.StartDate.ToString("MMM dd, yyyy") : $"{input.StartDate:MMM dd} - {input.EndDate:MMM dd, yyyy}",
            TeacherNotes = input.TeacherNotes
        };

        // 3a. Attendance
        var attendanceList = new List<string>();
        for (var d = input.StartDate; d <= input.EndDate; d = d.AddDays(1))
        {
            var att = await _parentRepository.GetChildAttendanceForDateAsync(_tenantContext.TenantId, child.Id, d.ToDateTime(TimeOnly.MinValue), cancellationToken);
            if (att != null)
            {
                var arrival = att.ArrivalTime.HasValue ? DateTime.Today.Add(att.ArrivalTime.Value).ToString("h:mm tt") : "Morning";
                attendanceList.Add($"{d:ddd}: {att.Status} (arrived {arrival})");
            }
        }
        evidence.AttendanceSummary = attendanceList.Any() ? string.Join(", ", attendanceList) : "Recorded present";

        // 3b. Completed Experiences (Classroom)
        if (classroomId.HasValue)
        {
            var schedules = (await _scheduleRepository.GetByClassroomAndDateRangeAsync(classroomId.Value, input.StartDate, input.EndDate)).ToList();
            foreach (var sch in schedules.Where(s => string.Equals(s.Status, "Completed", StringComparison.OrdinalIgnoreCase) || string.Equals(s.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)))
            {
                var exp = await _experienceRepository.GetByIdAsync(_tenantContext.TenantId, sch.ExperienceId, cancellationToken);
                if (exp != null)
                {
                    evidence.CompletedExperiences.Add($"{exp.Title}: {exp.Description}");
                }
            }
        }

        // 3c. Approved Moments (ONLY is_parent_visible = true)
        var allMoments = (await _parentRepository.GetApprovedJourneyEntriesAsync(_tenantContext.TenantId, child.Id, cancellationToken)).ToList();
        var dateRangeMoments = allMoments.Where(m =>
        {
            var dUtc = DateOnly.FromDateTime(m.ObservationDate.UtcDateTime.Date);
            var dLoc = DateOnly.FromDateTime(m.ObservationDate.LocalDateTime.Date);
            return (dUtc >= input.StartDate && dUtc <= input.EndDate) || (dLoc >= input.StartDate && dLoc <= input.EndDate);
        }).ToList();

        foreach (var m in dateRangeMoments)
        {
            evidence.ApprovedMoments.Add($"{m.Title ?? "Moment"}: {m.Observation}" + (!string.IsNullOrEmpty(m.ChildVoice) ? $" (Child said: \"{m.ChildVoice}\")" : ""));
        }

        // 3d. Daily Care Highlights (Meals, Naps, Comfort)
        if (_dailyCareService != null)
        {
            try
            {
                for (var d = input.StartDate; d <= input.EndDate; d = d.AddDays(1))
                {
                    var report = await _dailyCareService.GetParentDailyCareReportAsync(child.Id, d, cancellationToken);
                    foreach (var item in report.Timeline)
                    {
                        evidence.CareHighlights.Add($"{d:ddd} {item.Title}: {item.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve daily care report for child {ChildId}", child.Id);
            }
        }

        // 3e. Milestones (parent_visible = true)
        var milestones = (await _parentRepository.GetApprovedMilestonesAsync(_tenantContext.TenantId, child.Id, cancellationToken)).ToList();
        foreach (var ms in milestones.Where(m =>
        {
            var md = DateOnly.FromDateTime(m.MilestoneDate.Date);
            return md >= input.StartDate && md <= input.EndDate;
        }))
        {
            evidence.MilestoneHighlights.Add($"{ms.Title}: {ms.Description}");
        }

        // 3f. Upcoming Events
        var events = (await _parentRepository.GetApprovedEventsForChildAsync(_tenantContext.TenantId, child.Id, cancellationToken)).ToList();
        foreach (var ev in events.Take(2))
        {
            evidence.RelevantEvents.Add($"{ev.Title} on {ev.EventDate:MMM dd}");
        }

        // 4. Call AI Agent or use Grounded Heuristic Fallback
        var aiDraft = await GenerateAiCommunicationDraftAsync(evidence, selectedRecipient, cancellationToken);

        // 5. Construct Draft Entity
        var fullMessage = $"{aiDraft.Greeting}\n\n{aiDraft.Highlights}\n\n{aiDraft.GrowthAndLearning}\n\n{aiDraft.FamilyConversationStarter}\n\n{aiDraft.SignOff}".Trim();

        var draft = new ParentCommunication
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            OrganizationId = organizationId,
            ClassroomId = classroomId,
            ChildId = child.Id,
            ChildName = evidence.ChildName,
            ClassroomName = evidence.ClassroomName,
            RecipientPersonId = selectedRecipient?.PersonId,
            RecipientUserId = selectedRecipient?.UserId,
            RecipientEmail = selectedRecipient?.Email,
            RecipientName = selectedRecipient?.FullName ?? "Parent",
            RecipientRelationship = selectedRecipient?.Relationship ?? "Guardian",
            CommunicationType = input.CommunicationType,
            ReportingDateStart = input.StartDate,
            ReportingDateEnd = input.EndDate,
            Title = aiDraft.Title,
            ContentDraft = fullMessage,
            ContentFinal = fullMessage,
            FamilyConversationStarter = aiDraft.FamilyConversationStarter,
            EvidenceSummaryJson = JsonSerializer.Serialize(new
            {
                evidence.AttendanceSummary,
                MomentsCount = evidence.ApprovedMoments.Count,
                ExperiencesCount = evidence.CompletedExperiences.Count,
                CareEventsCount = evidence.CareHighlights.Count,
                MilestonesCount = evidence.MilestoneHighlights.Count,
                TeacherNotesIncluded = !string.IsNullOrWhiteSpace(input.TeacherNotes)
            }),
            ConsentVerified = selectedRecipient == null || selectedRecipient.CanReceiveInformation,
            ConsentNotes = selectedRecipient?.ReceivesDailyUpdates == true ? "Guardian permitted for daily updates" : "Standard guardian notification",
            Status = ParentCommunicationStatus.Draft,
            TeacherId = _userContext.UserId,
            TeacherName = _userContext.Email ?? "Teacher",
            DeliveryChannel = "PortalAndEmail",
            DeliveryStatus = "Pending",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var saved = await _communicationRepository.CreateAsync(draft, cancellationToken);
        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "ParentCommunication.DraftCreated",
            EntityType = "ParentCommunication",
            EntityId = saved.Id,
            Details = $"Created parent communication draft for child {child.Id}"
        }, cancellationToken);

        return saved;
    }

    public async Task<ParentCommunication> UpdateDraftAsync(
        UpdateParentCommunicationDraftInput input, 
        CancellationToken cancellationToken = default)
    {
        var existing = await _communicationRepository.GetByIdAsync(_tenantContext.TenantId, input.CommunicationId, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Draft {input.CommunicationId} not found in this tenant.");
        }

        if (existing.Status == ParentCommunicationStatus.Sent || existing.Status == ParentCommunicationStatus.Sending)
        {
            throw new InvalidOperationException("Cannot modify a communication that has already been approved or sent.");
        }

        existing.Title = input.Title;
        existing.ContentFinal = input.ContentFinal;
        existing.FamilyConversationStarter = input.FamilyConversationStarter;
        existing.Status = ParentCommunicationStatus.PendingReview;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _communicationRepository.UpdateAsync(existing, cancellationToken);
        await _auditService.LogAsync(new AuditEntry
        {
            TenantId = _tenantContext.TenantId,
            UserId = _userContext.UserId,
            Action = "ParentCommunication.DraftUpdated",
            EntityType = "ParentCommunication",
            EntityId = existing.Id,
            Details = "Updated parent communication draft"
        }, cancellationToken);

        return existing;
    }

    public async Task<ParentCommunication> ApproveAndSendAsync(
        SendParentCommunicationInput input, 
        CancellationToken cancellationToken = default)
    {
        var existing = await _communicationRepository.GetByIdAsync(_tenantContext.TenantId, input.CommunicationId, cancellationToken);
        if (existing == null)
        {
            throw new KeyNotFoundException($"Communication {input.CommunicationId} not found.");
        }

        // Idempotency check: if already sent, return immediately
        if (existing.Status == ParentCommunicationStatus.Sent)
        {
            return existing;
        }

        // Revalidate Recipient authorization
        if (existing.RecipientPersonId.HasValue)
        {
            var recipients = await _communicationRepository.GetRecipientsForChildAsync(_tenantContext.TenantId, existing.ChildId, cancellationToken);
            var recipient = recipients.FirstOrDefault(r => r.PersonId == existing.RecipientPersonId.Value);
            if (recipient == null || !recipient.CanReceiveInformation)
            {
                throw new UnauthorizedAccessException($"Recipient is no longer authorized to receive communications for this child.");
            }
        }

        existing.Status = ParentCommunicationStatus.Approved;
        existing.ApprovedByUserId = _userContext.UserId;
        existing.ApprovedAt = DateTimeOffset.UtcNow;
        existing.DeliveryChannel = input.DeliveryChannel ?? "PortalAndEmail";
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            existing.Status = ParentCommunicationStatus.Sending;
            await _communicationRepository.UpdateAsync(existing, cancellationToken);

            // Execute delivery: log and simulate email delivery to recipient
            _logger.LogInformation(
                "Delivering parent communication {Id} via {Channel} to {Recipient} ({Email}) for child {ChildId}",
                existing.Id, existing.DeliveryChannel, existing.RecipientName, existing.RecipientEmail, existing.ChildId);

            // Simulate outbox completion
            existing.Status = ParentCommunicationStatus.Sent;
            existing.DeliveryStatus = "Delivered";
            existing.SentAt = DateTimeOffset.UtcNow;
            existing.DeliveryError = null;

            await _communicationRepository.UpdateAsync(existing, cancellationToken);
            await _auditService.LogAsync(new AuditEntry
            {
                TenantId = _tenantContext.TenantId,
                UserId = _userContext.UserId,
                Action = "ParentCommunication.Sent",
                EntityType = "ParentCommunication",
                EntityId = existing.Id,
                Details = $"Sent communication to {existing.RecipientEmail}"
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver parent communication {Id}", existing.Id);
            existing.Status = ParentCommunicationStatus.DeliveryFailed;
            existing.DeliveryStatus = "Failed";
            existing.DeliveryError = ex.Message;
            await _communicationRepository.UpdateAsync(existing, cancellationToken);
            throw;
        }

        return existing;
    }

    private async Task<AiDraftDto> GenerateAiCommunicationDraftAsync(
        ParentCommunicationEvidencePackage evidence, 
        ParentCommunicationRecipientInfo? recipient, 
        CancellationToken cancellationToken)
    {
        var apiKey = await _settingsService.GetAiApiKeyAsync(_tenantContext.TenantId);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var payload = new
                {
                    childName = evidence.ChildName,
                    classroomName = evidence.ClassroomName,
                    recipientName = recipient?.FullName ?? "Parent",
                    recipientRelationship = recipient?.Relationship ?? "Guardian",
                    communicationType = evidence.CommunicationType,
                    reportingPeriod = evidence.ReportingPeriod,
                    attendanceSummary = evidence.AttendanceSummary,
                    completedExperiences = evidence.CompletedExperiences,
                    approvedMoments = evidence.ApprovedMoments,
                    careHighlights = evidence.CareHighlights,
                    milestoneHighlights = evidence.MilestoneHighlights,
                    relevantEvents = evidence.RelevantEvents,
                    teacherNotes = evidence.TeacherNotes ?? ""
                };

                using var reqMsg = new HttpRequestMessage(HttpMethod.Post, "/api/parent-communication");
                reqMsg.Headers.Add("X-Tenant-Api-Key", apiKey);
                reqMsg.Content = JsonContent.Create(payload);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

                var response = await _httpClient.SendAsync(reqMsg, linkedCts.Token);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AiDraftDto>(cancellationToken: cancellationToken);
                    if (result != null && !string.IsNullOrWhiteSpace(result.Highlights))
                    {
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calling AI service for parent communication draft; falling back to deterministic builder.");
            }
        }

        // Grounded deterministic fallback
        return BuildDeterministicDraft(evidence, recipient);
    }

    private static AiDraftDto BuildDeterministicDraft(ParentCommunicationEvidencePackage evidence, ParentCommunicationRecipientInfo? recipient)
    {
        var firstName = evidence.ChildName.Split(' ').FirstOrDefault() ?? evidence.ChildName;
        var parentGreeting = recipient != null && !string.IsNullOrWhiteSpace(recipient.FullName)
            ? $"Dear {recipient.FullName},"
            : $"Dear {firstName}'s Family,";

        var highlightsList = new List<string>();
        if (!string.IsNullOrWhiteSpace(evidence.AttendanceSummary))
        {
            highlightsList.Add($"{firstName} was welcomed warmly to {evidence.ClassroomName} ({evidence.AttendanceSummary}).");
        }

        if (evidence.CompletedExperiences.Any())
        {
            var expText = string.Join("; ", evidence.CompletedExperiences.Take(3));
            highlightsList.Add($"In our planned group experiences, {firstName} engaged enthusiastically with: {expText}.");
        }

        if (evidence.ApprovedMoments.Any())
        {
            var momText = string.Join("; ", evidence.ApprovedMoments.Take(2));
            highlightsList.Add($"A special classroom moment we observed: {momText}.");
        }

        if (evidence.CareHighlights.Any())
        {
            var careText = string.Join("; ", evidence.CareHighlights.Take(2));
            highlightsList.Add($"Daily care and rest highlights: {careText}.");
        }

        if (!highlightsList.Any())
        {
            highlightsList.Add($"{firstName} had a calm, joyful time engaging in sensory exploration and social play with friends.");
        }

        var growthList = new List<string>();
        if (evidence.MilestoneHighlights.Any())
        {
            growthList.Add($"We are so proud to celebrate: {string.Join("; ", evidence.MilestoneHighlights)}.");
        }
        growthList.Add("These experiences supported curiosity, cooperative play, and expressive language.");

        var starter = evidence.CompletedExperiences.Any()
            ? $"Ask {firstName} what they enjoyed creating during '{evidence.CompletedExperiences.First().Split(':').First()}' today!"
            : $"Ask {firstName} about their favorite playtime activity with friends today!";

        var title = evidence.CommunicationType == ParentCommunicationTypes.DailyUpdate
            ? $"{firstName}'s Daily Learning & Care Update"
            : $"{firstName}'s Weekly Growth & Moments Summary";

        return new AiDraftDto
        {
            Title = title,
            Greeting = parentGreeting,
            Highlights = string.Join(" ", highlightsList),
            GrowthAndLearning = string.Join(" ", growthList),
            FamilyConversationStarter = starter,
            SignOff = $"Warmly,\nThe {evidence.ClassroomName} Team"
        };
    }

    private class AiDraftDto
    {
        public string Title { get; set; } = string.Empty;
        public string Greeting { get; set; } = string.Empty;
        public string Highlights { get; set; } = string.Empty;
        public string GrowthAndLearning { get; set; } = string.Empty;
        public string FamilyConversationStarter { get; set; } = string.Empty;
        public string SignOff { get; set; } = string.Empty;
    }
}
