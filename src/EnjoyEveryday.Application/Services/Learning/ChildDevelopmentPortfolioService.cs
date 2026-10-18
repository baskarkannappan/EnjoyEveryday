using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Shared.Tenancy;

namespace EnjoyEveryday.Application.Services;

public enum MediaConsentStatus
{
    Granted,
    Restricted,
    Pending,
    Missing
}

public class PortfolioConsentSummary
{
    public MediaConsentStatus Status { get; set; } = MediaConsentStatus.Missing;
    public string StatusLabel { get; set; } = "Consent Missing";
    public string Description { get; set; } = "No media release on file. Media must remain internal.";
    public bool CanSharePhotosWithParents => Status == MediaConsentStatus.Granted;
    public DateTimeOffset? SignedAt { get; set; }
}

public class PortfolioDomainProgress
{
    public Guid DomainId { get; set; }
    public string DomainName { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-journal";
    public int DisplayOrder { get; set; }
    public int EvidenceCount { get; set; }
    public int MilestonesCount { get; set; }
    public int Percentage { get; set; }
    public string StatusNote { get; set; } = "Active";
}

public class PortfolioItem
{
    public Guid Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public string DateString => Date.ToString("MMM d, yyyy");
    public string TimeString => Date.ToString("h:mm tt");
    public string Category { get; set; } = "Observation"; // Observation, Milestone, Experience, Moment
    public string BadgeType { get; set; } = "discovery";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ChildVoice { get; set; }
    public string? TeacherReflection { get; set; }
    public string? TeacherName { get; set; }
    public List<string> Domains { get; set; } = new();
    public List<string> DomainIcons { get; set; } = new();
    public bool IsParentVisible { get; set; }
    public string VisibilityStatus { get; set; } = "InternalOnly";
    public List<string> Photos { get; set; } = new();
    public bool IsMediaBlockedByConsent { get; set; }
    public bool CanDelete { get; set; }
}

internal class DomainCountRecord
{
    public int Evidence { get; set; }
    public int Milestones { get; set; }
}

public class ChildPortfolioSummary
{
    public Guid ChildId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string AgeString { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public int TotalMoments { get; set; }
    public int TotalMilestones { get; set; }
    public int TotalExperiences { get; set; }
    public string CurrentFocusDomain { get; set; } = "All Round";
    public PortfolioConsentSummary Consent { get; set; } = new();
    public List<PortfolioDomainProgress> Domains { get; set; } = new();
    public List<PortfolioItem> Items { get; set; } = new();
    public List<string> ConferenceStrengths { get; set; } = new();
    public List<string> ConferenceNextSteps { get; set; } = new();
}

public interface IChildDevelopmentPortfolioService
{
    Task<ChildPortfolioSummary?> GetChildPortfolioAsync(
        Guid childId,
        string? domainFilter = null,
        string? visibilityFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);

    Task<PortfolioConsentSummary> CheckMediaConsentAsync(Guid childId, CancellationToken ct = default);
}

public class ChildDevelopmentPortfolioService : IChildDevelopmentPortfolioService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext? _userContext;

    public ChildDevelopmentPortfolioService(
        IDbConnectionFactory connectionFactory,
        ITenantContext tenantContext,
        IUserContext? userContext = null)
    {
        _connectionFactory = connectionFactory;
        _tenantContext = tenantContext;
        _userContext = userContext;
    }

    public async Task<PortfolioConsentSummary> CheckMediaConsentAsync(Guid childId, CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        
        var sql = @"
            SELECT d.id, d.status, d.document_title, d.signed_at, d.withdrawn_at, d.declined_at,
                   COALESCE(t.document_type, '') AS DocumentType
            FROM child_consent_documents d
            LEFT JOIN consent_templates t ON d.consent_template_id = t.id
            WHERE d.tenant_id = @TenantId AND d.child_id = @ChildId
              AND (t.document_type = 'PhotoVideoConsent' OR d.document_title ILIKE '%Photo%' OR d.document_title ILIKE '%Media%')
            ORDER BY d.created_at DESC
            LIMIT 1";

        var doc = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { TenantId = _tenantContext.TenantId, ChildId = childId });

        if (doc == null)
        {
            return new PortfolioConsentSummary
            {
                Status = MediaConsentStatus.Missing,
                StatusLabel = "Consent Missing",
                Description = "No photo/video release on file for this child. Media is restricted from public/parent sharing."
            };
        }

        string status = (string)(doc.status ?? "Draft");
        DateTime? withdrawnAt = doc.withdrawn_at;
        DateTime? declinedAt = doc.declined_at;
        DateTimeOffset? signedAt = doc.signed_at;

        if (withdrawnAt != null || status.Equals("Withdrawn", StringComparison.OrdinalIgnoreCase))
        {
            return new PortfolioConsentSummary
            {
                Status = MediaConsentStatus.Restricted,
                StatusLabel = "Consent Withdrawn",
                Description = "Parent revoked photo and media permissions. All media must remain internal.",
                SignedAt = signedAt
            };
        }

        if (declinedAt != null || status.Equals("Declined", StringComparison.OrdinalIgnoreCase))
        {
            return new PortfolioConsentSummary
            {
                Status = MediaConsentStatus.Restricted,
                StatusLabel = "Consent Declined",
                Description = "Parent declined photo and media permissions. All media must remain internal.",
                SignedAt = signedAt
            };
        }

        if (status.Equals("Signed", StringComparison.OrdinalIgnoreCase))
        {
            // Verify if any negative choice was recorded
            var choicesSql = @"
                SELECT choice FROM consent_choices 
                WHERE tenant_id = @TenantId AND consent_document_id = @DocId
                  AND (section_key ILIKE '%photo%' OR section_key ILIKE '%media%')";
            
            var choices = (await conn.QueryAsync<string>(choicesSql, new { TenantId = _tenantContext.TenantId, DocId = (Guid)doc.id })).ToList();
            bool hasRefusal = choices.Any(c => c.Equals("Declined", StringComparison.OrdinalIgnoreCase) || c.Equals("No", StringComparison.OrdinalIgnoreCase));

            if (hasRefusal)
            {
                return new PortfolioConsentSummary
                {
                    Status = MediaConsentStatus.Restricted,
                    StatusLabel = "Photo Consent Opted-Out",
                    Description = "Parent explicitly opted out of photo sharing in signed agreement.",
                    SignedAt = signedAt
                };
            }

            return new PortfolioConsentSummary
            {
                Status = MediaConsentStatus.Granted,
                StatusLabel = "Photo Consent Active",
                Description = "Parent signed Photo & Media Release. Safe for learning journey & portfolio sharing.",
                SignedAt = signedAt
            };
        }

        if (status.Equals("Sent", StringComparison.OrdinalIgnoreCase) || status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            return new PortfolioConsentSummary
            {
                Status = MediaConsentStatus.Pending,
                StatusLabel = "Consent Pending Signature",
                Description = "Consent document has been sent to parent but is awaiting electronic signature.",
                SignedAt = signedAt
            };
        }

        return new PortfolioConsentSummary
        {
            Status = MediaConsentStatus.Missing,
            StatusLabel = "Draft / Not Sent",
            Description = "Consent document is still in draft state.",
            SignedAt = signedAt
        };
    }

    public async Task<ChildPortfolioSummary?> GetChildPortfolioAsync(
        Guid childId,
        string? domainFilter = null,
        string? visibilityFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateConnectionAsync(ct);

        // 1. Fetch child profile
        var childSql = @"
            SELECT c.id, c.first_name AS FirstName, c.last_name AS LastName, c.photo_url AS PhotoUrl, c.date_of_birth AS DateOfBirth,
                   cr.name AS ClassroomName, b.name AS BranchName
            FROM children c
            LEFT JOIN classrooms cr ON c.classroom_id = cr.id
            LEFT JOIN branches b ON cr.branch_id = b.id
            WHERE c.tenant_id = @TenantId AND c.id = @ChildId";

        var child = await conn.QueryFirstOrDefaultAsync<dynamic>(childSql, new { TenantId = _tenantContext.TenantId, ChildId = childId });
        if (child == null) return null;

        var consent = await CheckMediaConsentAsync(childId, ct);

        // 2. Fetch standard development areas
        var areasSql = @"
            SELECT id, name, icon, display_order AS DisplayOrder
            FROM development_areas
            WHERE (tenant_id IS NULL OR tenant_id = @TenantId) AND is_active = TRUE
            ORDER BY display_order";

        var allAreas = (await conn.QueryAsync<dynamic>(areasSql, new { TenantId = _tenantContext.TenantId })).ToList();
        var areaDict = allAreas.ToDictionary(a => (Guid)a.id, a => (string)a.name);
        var areaIconDict = allAreas.ToDictionary(a => (Guid)a.id, a => (string)a.icon);

        // 3. Fetch Journey Entries
        var entriesSql = @"
            SELECT je.id, je.observation_date AS ObservationDate, je.journey_type AS JourneyType,
                   je.title, je.observation, je.child_voice AS ChildVoice, je.teacher_reflection AS TeacherReflection,
                   je.is_milestone AS IsMilestone, je.is_parent_visible AS IsParentVisible,
                   je.parent_visibility_status AS ParentVisibilityStatus,
                   COALESCE(t.first_name || ' ' || t.last_name, 'Teacher') AS TeacherName
            FROM child_journey_entries je
            LEFT JOIN teachers t ON je.teacher_id = t.id
            WHERE je.tenant_id = @TenantId AND je.child_id = @ChildId
            ORDER BY je.observation_date DESC";

        var rawEntries = (await conn.QueryAsync<dynamic>(entriesSql, new { TenantId = _tenantContext.TenantId, ChildId = childId })).ToList();

        // 4. Fetch linked development areas for entries
        var entryAreasSql = @"
            SELECT d.journey_entry_id, d.development_area_id, a.name, a.icon
            FROM child_journey_developments d
            JOIN development_areas a ON d.development_area_id = a.id
            WHERE d.journey_entry_id IN (
                SELECT id FROM child_journey_entries WHERE tenant_id = @TenantId AND child_id = @ChildId
            )";

        var entryAreasRows = (await conn.QueryAsync<dynamic>(entryAreasSql, new { TenantId = _tenantContext.TenantId, ChildId = childId })).ToList();
        var entryAreasGrouped = entryAreasRows.GroupBy(r => (Guid)r.journey_entry_id)
            .ToDictionary(g => g.Key, g => g.Select(x => new { Name = (string)x.name, Icon = (string)x.icon }).ToList());

        // 5. Fetch evidence photos for entries
        var evidenceSql = @"
            SELECT journey_entry_id, text_content
            FROM child_journey_evidence
            WHERE journey_entry_id IN (
                SELECT id FROM child_journey_entries WHERE tenant_id = @TenantId AND child_id = @ChildId
            )
            ORDER BY created_at DESC";

        var evidenceRows = (await conn.QueryAsync<dynamic>(evidenceSql, new { TenantId = _tenantContext.TenantId, ChildId = childId })).ToList();
        var evidenceGrouped = evidenceRows.GroupBy(r => (Guid)r.journey_entry_id)
            .ToDictionary(g => g.Key, g => g.Select(x => (string)x.text_content).Where(s => !string.IsNullOrWhiteSpace(s)).ToList());

        // 6. Fetch Milestones
        var milestonesSql = @"
            SELECT m.id, m.milestone_date AS MilestoneDate, m.title, m.description, m.evidence_summary AS EvidenceSummary,
                   m.parent_visible AS ParentVisible, m.status, m.development_area_id AS DevelopmentAreaId,
                   COALESCE(t.first_name || ' ' || t.last_name, 'Teacher') AS TeacherName
            FROM child_milestones m
            LEFT JOIN teachers t ON m.teacher_id = t.id
            WHERE m.tenant_id = @TenantId AND m.child_id = @ChildId
            ORDER BY m.milestone_date DESC";

        var rawMilestones = (await conn.QueryAsync<dynamic>(milestonesSql, new { TenantId = _tenantContext.TenantId, ChildId = childId })).ToList();

        // 7. Fetch Experience Participations
        var experiencesSql = @"
            SELECT p.id, s.session_date + COALESCE(s.end_time, s.start_time, '00:00:00'::time) AS SessionDate,
                   e.title AS ExperienceTitle, COALESCE(p.participation_notes, s.actual_notes) AS Notes,
                   COALESCE(t.first_name || ' ' || t.last_name, 'Teacher') AS TeacherName
            FROM experience_participations p
            JOIN experience_sessions s ON p.experience_session_id = s.id
            JOIN experiences e ON s.experience_id = e.id
            LEFT JOIN teachers t ON s.primary_teacher_id = t.id
            WHERE s.tenant_id = @TenantId AND p.child_id = @ChildId AND p.participation_status = 'Participated'
            ORDER BY s.session_date DESC";

        var rawExperiences = (await conn.QueryAsync<dynamic>(experiencesSql, new { TenantId = _tenantContext.TenantId, ChildId = childId })).ToList();

        // Assemble unified timeline
        var items = new List<PortfolioItem>();

        // Add journey entries
        foreach (var e in rawEntries)
        {
            Guid entryId = (Guid)e.id;
            DateTimeOffset date = (DateTimeOffset)e.ObservationDate;
            bool isParentVis = (bool)e.IsParentVisible;
            bool isMilestone = (bool)e.IsMilestone;
            string jType = (string)(e.JourneyType ?? "Observation");

            var domainList = new List<string>();
            var domainIcons = new List<string>();
            if (entryAreasGrouped.TryGetValue(entryId, out var mappedDomains))
            {
                domainList.AddRange(mappedDomains.Select(d => d.Name));
                domainIcons.AddRange(mappedDomains.Select(d => d.Icon));
            }

            var photos = new List<string>();
            if (evidenceGrouped.TryGetValue(entryId, out var pList))
            {
                photos.AddRange(pList);
            }

            bool isMediaBlocked = photos.Count > 0 && !consent.CanSharePhotosWithParents;

            items.Add(new PortfolioItem
            {
                Id = entryId,
                Date = date,
                Category = isMilestone ? "Milestone" : (jType.Equals("Moment", StringComparison.OrdinalIgnoreCase) ? "Moment" : "Observation"),
                BadgeType = isMilestone ? "milestone" : jType.ToLower(),
                Title = (string)(e.title ?? (isMilestone ? "Milestone Achieved" : "Observation")),
                Description = (string)(e.observation ?? ""),
                ChildVoice = (string?)e.ChildVoice,
                TeacherReflection = (string?)e.TeacherReflection,
                TeacherName = (string)e.TeacherName,
                Domains = domainList,
                DomainIcons = domainIcons,
                IsParentVisible = isParentVis,
                VisibilityStatus = isParentVis ? "ParentVisible" : "InternalOnly",
                Photos = photos,
                IsMediaBlockedByConsent = isMediaBlocked,
                CanDelete = true
            });
        }

        // Add standalone milestones
        foreach (var m in rawMilestones)
        {
            Guid mId = (Guid)m.id;
            DateTimeOffset date = (DateTimeOffset)m.MilestoneDate;
            bool isParentVis = (bool)m.ParentVisible;
            Guid? devAreaId = (Guid?)m.DevelopmentAreaId;

            var domainList = new List<string>();
            var domainIcons = new List<string>();
            if (devAreaId.HasValue && areaDict.TryGetValue(devAreaId.Value, out var aName))
            {
                domainList.Add(aName);
                if (areaIconDict.TryGetValue(devAreaId.Value, out var aIcon))
                {
                    domainIcons.Add(aIcon);
                }
            }

            items.Add(new PortfolioItem
            {
                Id = mId,
                Date = date,
                Category = "Milestone",
                BadgeType = "milestone",
                Title = (string)(m.title ?? "Milestone Achieved"),
                Description = (string)(m.description ?? m.EvidenceSummary ?? "Developmental milestone achieved."),
                TeacherName = (string)m.TeacherName,
                Domains = domainList,
                DomainIcons = domainIcons,
                IsParentVisible = isParentVis,
                VisibilityStatus = isParentVis ? "ParentVisible" : "InternalOnly",
                Photos = new List<string>(),
                IsMediaBlockedByConsent = false,
                CanDelete = false
            });
        }

        // Add experience participations
        foreach (var exp in rawExperiences)
        {
            Guid pId = (Guid)exp.id;
            DateTimeOffset date = (DateTimeOffset)exp.SessionDate;

            items.Add(new PortfolioItem
            {
                Id = pId,
                Date = date,
                Category = "Experience",
                BadgeType = "experience",
                Title = (string)(exp.ExperienceTitle ?? "Learning Experience"),
                Description = (string)(exp.Notes ?? "Actively participated in classroom learning experience."),
                TeacherName = (string)exp.TeacherName,
                Domains = new List<string> { "Cognitive & Problem Solving" },
                DomainIcons = new List<string> { "bi-lightbulb" },
                IsParentVisible = true,
                VisibilityStatus = "ParentVisible",
                Photos = new List<string>(),
                IsMediaBlockedByConsent = false,
                CanDelete = false
            });
        }

        // Apply chronological sort
        items = items.OrderByDescending(i => i.Date).ToList();

        // Calculate domain counts across all items
        var domainCounts = new Dictionary<string, DomainCountRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var area in allAreas)
        {
            domainCounts[(string)area.name] = new DomainCountRecord();
        }

        foreach (var it in items)
        {
            foreach (var d in it.Domains)
            {
                if (domainCounts.TryGetValue(d, out var curr))
                {
                    curr.Evidence++;
                    if (it.Category == "Milestone")
                    {
                        curr.Milestones++;
                    }
                }
            }
        }

        int totalObservations = items.Count(i => i.Category != "Milestone");
        int totalMilestonesCount = items.Count(i => i.Category == "Milestone");
        int totalExperiencesCount = rawExperiences.Count;

        var domainProgressList = new List<PortfolioDomainProgress>();
        foreach (var area in allAreas)
        {
            string aName = (string)area.name;
            var counts = domainCounts.TryGetValue(aName, out var c) ? c : new DomainCountRecord();
            int percentage = items.Count > 0 ? (int)Math.Round((double)counts.Evidence / items.Count * 100) : 0;

            string statusNote = counts.Evidence >= 5 ? "Consistent Progress" :
                                counts.Evidence >= 2 ? "Emerging Skills" : "Observing";

            domainProgressList.Add(new PortfolioDomainProgress
            {
                DomainId = (Guid)area.id,
                DomainName = aName,
                Icon = (string)area.icon,
                DisplayOrder = (int)area.DisplayOrder,
                EvidenceCount = counts.Evidence,
                MilestonesCount = counts.Milestones,
                Percentage = percentage,
                StatusNote = statusNote
            });
        }

        string topDomain = domainProgressList.OrderByDescending(d => d.EvidenceCount).FirstOrDefault()?.DomainName ?? "Balanced Learning";

        // Filtering for display
        var filteredItems = items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(domainFilter) && !domainFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            filteredItems = filteredItems.Where(i => i.Domains.Any(d => d.Equals(domainFilter, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(visibilityFilter) && !visibilityFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (visibilityFilter.Equals("parent", StringComparison.OrdinalIgnoreCase))
            {
                filteredItems = filteredItems.Where(i => i.IsParentVisible);
            }
            else if (visibilityFilter.Equals("internal", StringComparison.OrdinalIgnoreCase))
            {
                filteredItems = filteredItems.Where(i => !i.IsParentVisible);
            }
        }

        if (fromDate.HasValue)
        {
            filteredItems = filteredItems.Where(i => i.Date.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            filteredItems = filteredItems.Where(i => i.Date.Date <= toDate.Value.Date);
        }

        // Derive conference narrative strengths & next steps deterministically
        var strengths = new List<string>();
        var nextSteps = new List<string>();

        var topDomains = domainProgressList.Where(d => d.EvidenceCount > 0).OrderByDescending(d => d.EvidenceCount).Take(2).ToList();
        foreach (var td in topDomains)
        {
            strengths.Add($"Shows marked enthusiasm and active curiosity in {td.DomainName} ({td.EvidenceCount} recorded observations).");
        }
        if (totalMilestonesCount > 0)
        {
            strengths.Add($"Achieved {totalMilestonesCount} developmental milestones during this period.");
        }
        else
        {
            strengths.Add("Engages enthusiastically in daily classroom routines and group sensory activities.");
        }

        var lowerDomains = domainProgressList.Where(d => d.EvidenceCount < 3).OrderBy(d => d.EvidenceCount).Take(2).ToList();
        foreach (var ld in lowerDomains)
        {
            nextSteps.Add($"Continue scaffolding opportunities within {ld.DomainName} during small-group and free-play stations.");
        }
        if (nextSteps.Count == 0)
        {
            nextSteps.Add("Encourage peer mentorship and creative project leadership in upcoming seasonal experiences.");
        }

        DateTime? dob = child.DateOfBirth;
        string ageStr = "";
        if (dob.HasValue)
        {
            var today = DateTime.Today;
            int years = today.Year - dob.Value.Year;
            if (dob.Value.Date > today.AddYears(-years)) years--;
            int months = (today.Year - dob.Value.Year) * 12 + today.Month - dob.Value.Month;
            if (today.Day < dob.Value.Day) months--;
            int remainingMonths = months % 12;
            ageStr = years > 0 ? $"{years} yr {(remainingMonths > 0 ? $"{remainingMonths} mo" : "")}".Trim() : $"{months} months";
        }

        return new ChildPortfolioSummary
        {
            ChildId = childId,
            FullName = $"{child.FirstName} {child.LastName}".Trim(),
            PhotoUrl = (string?)child.PhotoUrl,
            DateOfBirth = dob,
            AgeString = ageStr,
            ClassroomName = (string)(child.ClassroomName ?? "Classroom"),
            BranchName = (string)(child.BranchName ?? "Main Branch"),
            TotalMoments = totalObservations,
            TotalMilestones = totalMilestonesCount,
            TotalExperiences = totalExperiencesCount,
            CurrentFocusDomain = topDomain,
            Consent = consent,
            Domains = domainProgressList,
            Items = filteredItems.ToList(),
            ConferenceStrengths = strengths,
            ConferenceNextSteps = nextSteps
        };
    }
}
