using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Repositories;
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
    private readonly IChildDevelopmentPortfolioRepository _portfolioRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IUserContext? _userContext;

    public ChildDevelopmentPortfolioService(
        IChildDevelopmentPortfolioRepository portfolioRepository,
        ITenantContext tenantContext,
        IUserContext? userContext = null)
    {
        _portfolioRepository = portfolioRepository ?? throw new ArgumentNullException(nameof(portfolioRepository));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _userContext = userContext;
    }

    public async Task<PortfolioConsentSummary> CheckMediaConsentAsync(Guid childId, CancellationToken ct = default)
    {
        var doc = await _portfolioRepository.GetLatestMediaConsentDocumentAsync(_tenantContext.TenantId, childId, ct);

        if (doc == null)
        {
            return new PortfolioConsentSummary
            {
                Status = MediaConsentStatus.Missing,
                StatusLabel = "Consent Missing",
                Description = "No photo/video release on file for this child. Media is restricted from public/parent sharing."
            };
        }

        string status = doc.Status ?? "Draft";
        DateTime? withdrawnAt = doc.WithdrawnAt;
        DateTime? declinedAt = doc.DeclinedAt;
        DateTimeOffset? signedAt = doc.SignedAt;

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
            var choices = await _portfolioRepository.GetConsentChoicesAsync(_tenantContext.TenantId, doc.Id, ct);
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
        var tenantId = _tenantContext.TenantId;

        // 1. Fetch child profile
        var child = await _portfolioRepository.GetChildProfileAsync(tenantId, childId, ct);
        if (child == null) return null;

        var consent = await CheckMediaConsentAsync(childId, ct);

        // 2. Fetch standard development areas
        var allAreas = await _portfolioRepository.GetDevelopmentAreasAsync(tenantId, ct);
        var areaDict = allAreas.ToDictionary(a => a.Id, a => a.Name);
        var areaIconDict = allAreas.ToDictionary(a => a.Id, a => a.Icon);

        // 3. Fetch Journey Entries
        var rawEntries = await _portfolioRepository.GetJourneyEntriesAsync(tenantId, childId, ct);

        // 4. Fetch linked development areas for entries
        var entryAreasRows = await _portfolioRepository.GetEntryDevelopmentAreasAsync(tenantId, childId, ct);
        var entryAreasGrouped = entryAreasRows.GroupBy(r => r.JourneyEntryId)
            .ToDictionary(g => g.Key, g => g.Select(x => new { Name = x.Name, Icon = x.Icon }).ToList());

        // 5. Fetch evidence photos for entries
        var evidenceRows = await _portfolioRepository.GetEntryEvidenceAsync(tenantId, childId, ct);
        var evidenceGrouped = evidenceRows.GroupBy(r => r.JourneyEntryId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.TextContent).Where(s => !string.IsNullOrWhiteSpace(s)).ToList());

        // 6. Fetch Milestones
        var rawMilestones = await _portfolioRepository.GetMilestonesAsync(tenantId, childId, ct);

        // 7. Fetch Experience Participations
        var rawExperiences = await _portfolioRepository.GetExperienceParticipationsAsync(tenantId, childId, ct);

        // Assemble unified timeline
        var items = new List<PortfolioItem>();

        // Add journey entries
        foreach (var e in rawEntries)
        {
            Guid entryId = e.Id;
            DateTimeOffset date = e.ObservationDate;
            bool isParentVis = e.IsParentVisible;
            bool isMilestone = e.IsMilestone;
            string jType = e.JourneyType ?? "Observation";

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
                Title = e.Title ?? (isMilestone ? "Milestone Achieved" : "Observation"),
                Description = e.Observation ?? "",
                ChildVoice = e.ChildVoice,
                TeacherReflection = e.TeacherReflection,
                TeacherName = e.TeacherName,
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
            Guid mId = m.Id;
            DateTimeOffset date = m.MilestoneDate;
            bool isParentVis = m.ParentVisible;
            Guid? devAreaId = m.DevelopmentAreaId;

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
                Title = m.Title ?? "Milestone Achieved",
                Description = m.Description ?? m.EvidenceSummary ?? "Developmental milestone achieved.",
                TeacherName = m.TeacherName,
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
            Guid pId = exp.Id;
            DateTimeOffset date = exp.SessionDate;

            items.Add(new PortfolioItem
            {
                Id = pId,
                Date = date,
                Category = "Experience",
                BadgeType = "experience",
                Title = exp.ExperienceTitle ?? "Learning Experience",
                Description = exp.Notes ?? "Actively participated in classroom learning experience.",
                TeacherName = exp.TeacherName,
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
            domainCounts[area.Name] = new DomainCountRecord();
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
            string aName = area.Name;
            var counts = domainCounts.TryGetValue(aName, out var c) ? c : new DomainCountRecord();
            int percentage = items.Count > 0 ? (int)Math.Round((double)counts.Evidence / items.Count * 100) : 0;

            string statusNote = counts.Evidence >= 5 ? "Consistent Progress" :
                                counts.Evidence >= 2 ? "Emerging Skills" : "Observing";

            domainProgressList.Add(new PortfolioDomainProgress
            {
                DomainId = area.Id,
                DomainName = aName,
                Icon = area.Icon,
                DisplayOrder = area.DisplayOrder,
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
