using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Domain.Entities;
using EnjoyEveryday.Domain.Entities.Learning;
using EnjoyEveryday.Domain.Repositories;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services;

public class PlannerService
{
    private readonly IExperienceScheduleRepository _scheduleRepository;
    private readonly IExperienceRepository? _experienceRepository;
    private readonly IEventRepository? _eventRepository;
    private readonly ITenantContext? _tenantContext;
    private readonly IUserContext _userContext;
    private readonly IMaterialMarketplaceService? _materialService;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IClassroomRepository? _classroomRepository;
    private readonly IBranchRepository? _branchRepository;
    private readonly ITeacherRepository? _teacherRepository;
    private readonly IAccessScopeRepository? _accessScopeRepository;
    private readonly Microsoft.Extensions.Logging.ILogger<PlannerService>? _logger;

    public PlannerService(IExperienceScheduleRepository scheduleRepository, IUserContext userContext)
        : this(scheduleRepository, null, null, null, userContext, null)
    {
    }

    public PlannerService(
        IExperienceScheduleRepository scheduleRepository,
        IExperienceRepository? experienceRepository,
        IEventRepository? eventRepository,
        ITenantContext? tenantContext,
        IUserContext userContext,
        IMaterialMarketplaceService? materialService)
        : this(scheduleRepository, experienceRepository, eventRepository, tenantContext, userContext, materialService, null, null, null, null)
    {
    }

    public PlannerService(
        IExperienceScheduleRepository scheduleRepository,
        IExperienceRepository? experienceRepository,
        IEventRepository? eventRepository,
        ITenantContext? tenantContext,
        IUserContext userContext,
        IMaterialMarketplaceService? materialService,
        IClassroomRepository? classroomRepository,
        IBranchRepository? branchRepository,
        ITeacherRepository? teacherRepository,
        IAccessScopeRepository? accessScopeRepository,
        Microsoft.Extensions.Logging.ILogger<PlannerService>? logger = null)
    {
        _scheduleRepository = scheduleRepository;
        _experienceRepository = experienceRepository;
        _eventRepository = eventRepository;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _materialService = materialService;
        _classroomRepository = classroomRepository;
        _branchRepository = branchRepository;
        _teacherRepository = teacherRepository;
        _accessScopeRepository = accessScopeRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<ExperienceSchedule>> GetMonthlyScheduleAsync(Guid classroomId, int year, int month)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        return await _scheduleRepository.GetByClassroomAndDateRangeAsync(classroomId, startDate, endDate);
    }

    public async Task<PlanningCalendarSummaryDto> GetMonthlyCalendarSummaryAsync(Guid classroomId, int year, int month, CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var schedules = (await _scheduleRepository.GetByClassroomAndDateRangeAsync(classroomId, startDate, endDate)).ToList();

        var tenantId = _tenantContext?.TenantId ?? Guid.Empty;
        var experiences = new Dictionary<Guid, Experience>();
        if (_experienceRepository != null && tenantId != Guid.Empty)
        {
            var expList = await _experienceRepository.GetAllAsync(tenantId, cancellationToken);
            experiences = expList.ToDictionary(e => e.Id);
        }

        var eventsSummary = new List<EventSummaryDto>();
        if (_eventRepository != null && tenantId != Guid.Empty)
        {
            try
            {
                var events = await _eventRepository.GetAllForTenantAsync(tenantId);
                var monthlyEvents = events.Where(e => e.EventDate.Year == year && e.EventDate.Month == month).ToList();

                foreach (var ev in monthlyEvents)
                {
                    var dto = new EventSummaryDto
                    {
                        Id = ev.Id,
                        Title = ev.Title,
                        Description = ev.Description,
                        EventDate = ev.EventDate,
                        StartTime = ev.StartTime,
                        EndTime = ev.EndTime,
                        Status = ev.Status,
                        Scope = ev.Scope,
                        BranchId = ev.BranchId,
                        IsOrganizationWide = ev.IsOrganizationWide
                    };

                    if (ev.Scope == "Classroom")
                    {
                        var crIds = await _eventRepository.GetEventClassroomsAsync(ev.Id);
                        dto.AssignedClassroomIds = crIds.ToList();
                    }

                    eventsSummary.Add(dto);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading events in PlannerService");
            }
        }

        // Compute rhythm dimensions, goals, materials, adaptations
        int exploreCount = 0;
        int createCount = 0;
        int togetherCount = 0;
        int moveCount = 0;
        var allGoals = new List<string>();
        var allMaterials = new List<string>();
        var allAdaptations = new List<string>();

        foreach (var s in schedules)
        {
            if (experiences.TryGetValue(s.ExperienceId, out var exp))
            {
                if (!string.IsNullOrWhiteSpace(exp.DnaPayload))
                {
                    try
                    {
                        var dna = JsonSerializer.Deserialize<ExperienceDna>(exp.DnaPayload, _jsonOptions);
                        if (dna != null)
                        {
                            if (!string.IsNullOrWhiteSpace(dna.Challenge)) exploreCount++;
                            if (!string.IsNullOrWhiteSpace(dna.Create)) createCount++;
                            if (!string.IsNullOrWhiteSpace(dna.Together)) togetherCount++;
                            if ((dna.Environment != null && dna.Environment.Contains("Outdoor", StringComparison.OrdinalIgnoreCase)) ||
                                (exp.Title != null && (exp.Title.Contains("Scavenger", StringComparison.OrdinalIgnoreCase) || exp.Title.Contains("Dance", StringComparison.OrdinalIgnoreCase) || exp.Title.Contains("Forest", StringComparison.OrdinalIgnoreCase))))
                            {
                                moveCount++;
                            }

                            if (dna.DevelopmentalGoals != null && dna.DevelopmentalGoals.Any())
                                allGoals.AddRange(dna.DevelopmentalGoals);
                            if (dna.Materials != null && dna.Materials.Any())
                                allMaterials.AddRange(dna.Materials);
                            if (dna.ExecutionGuide?.Adaptations != null && dna.ExecutionGuide.Adaptations.Any())
                                allAdaptations.AddRange(dna.ExecutionGuide.Adaptations);
                        }

                        using var doc = JsonDocument.Parse(exp.DnaPayload);
                        if (doc.RootElement.TryGetProperty("materials", out var matArr) && matArr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in matArr.EnumerateArray())
                            {
                                var mStr = item.GetString();
                                if (!string.IsNullOrWhiteSpace(mStr)) allMaterials.Add(mStr);
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        var result = new PlanningCalendarSummaryDto
        {
            ClassroomId = classroomId,
            Year = year,
            Month = month,
            Schedules = schedules,
            Experiences = experiences.Values.ToList(),
            Events = eventsSummary,
            ExploreCount = exploreCount,
            CreateCount = createCount,
            TogetherCount = togetherCount,
            MoveCount = moveCount
        };

        if (allGoals.Any())
        {
            var top = allGoals.GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();
            result.TopGoal = top.First();
            result.TopGoalsList = top;
        }
        else
        {
            result.TopGoal = "Tactile exploration and sensory vocabulary";
            result.TopGoalsList = new List<string> { "Color recognition and sorting", "Fine motor coordination using tools", "Cooperative play and turn-taking" };
        }

        if (allMaterials.Any())
        {
            result.CommonMaterial = allMaterials.GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "craft materials";
            result.AiSuggestionsList = new List<string>
            {
                $"Plan an activity centered around {result.CommonMaterial}",
                "Combine this with a new environment to spark curiosity"
            };
        }
        else
        {
            result.CommonMaterial = "natural materials";
            result.AiSuggestionsList = new List<string>
            {
                "Plan an activity centered around sensory textures",
                "Combine this with a new environment to spark curiosity"
            };
        }

        if (allAdaptations.Any())
        {
            result.AdaptationsList = allAdaptations.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
        }
        else
        {
            result.AdaptationsList = new List<string> { "Indoor obstacle course (Move)", "Storytime with props (Together)", "Sensory bins with water/sand (Explore)" };
        }

        return result;
    }

    public async Task<OrderMissingMaterialsResultDto> OrderMissingMaterialsForScheduleAsync(OrderMissingMaterialsRequest request, CancellationToken cancellationToken = default)
    {
        if (_materialService == null)
        {
            throw new InvalidOperationException("Material marketplace service is not configured.");
        }

        var readiness = await _materialService.CheckExperienceMaterialReadinessAsync(request.ExperienceId, request.ClassroomId ?? Guid.Empty, cancellationToken);
        var missing = readiness.Materials.Where(m => !m.IsAvailableInStock && !m.IsOrdered).ToList();
        if (!missing.Any())
        {
            return new OrderMissingMaterialsResultDto
            {
                Success = true,
                Message = "All materials are already accounted for in stock or on order!",
                UpdatedReadiness = readiness
            };
        }

        var targetOrgId = request.OrganizationId ?? _userContext.OrganizationId ?? Guid.Empty;
        var schedule = await _scheduleRepository.GetByIdAsync(request.ScheduleId);

        var orderReq = new CreateMaterialOrderRequest
        {
            OrganizationId = targetOrgId,
            ClassroomId = request.ClassroomId,
            PlannedExperienceId = request.ExperienceId,
            RequiredDeliveryDate = schedule?.ScheduledDate != null
                ? new DateTimeOffset(schedule.ScheduledDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                : DateTimeOffset.UtcNow.AddDays(3),
            DeliveryAddress = request.DeliveryAddress ?? "Main Daycare Center",
            Notes = $"Automated order from Planning Calendar for activity ID '{request.ExperienceId}'",
            Items = missing.Select(m => new CreateMaterialOrderItemRequest
            {
                ProductId = m.MatchedProductId ?? Guid.NewGuid(),
                Quantity = 1,
                FulfilmentMode = m.CanBorrow ? MaterialFulfilmentMode.Borrow : MaterialFulfilmentMode.Purchase,
                LoanDurationDays = m.CanBorrow ? 14 : 0
            }).ToList()
        };

        var order = await _materialService.SubmitMaterialOrderAsync(orderReq, cancellationToken);
        var updatedReadiness = await _materialService.CheckExperienceMaterialReadinessAsync(request.ExperienceId, request.ClassroomId ?? Guid.Empty, cancellationToken);

        return new OrderMissingMaterialsResultDto
        {
            Success = true,
            OrderNumber = order.OrderNumber,
            Message = $"Order {order.OrderNumber} submitted successfully! Central warehouse has received your request.",
            UpdatedReadiness = updatedReadiness
        };
    }

    public async Task<ExperienceSchedule?> GetScheduleAsync(Guid scheduleId)
    {
        return await _scheduleRepository.GetByIdAsync(scheduleId);
    }

    public async Task<IEnumerable<ExperienceSchedule>> GetSchedulesForDateAsync(DateOnly date)
    {
        return await _scheduleRepository.GetByDateAsync(date);
    }

    public async Task<ExperienceSchedule> ScheduleExperienceAsync(Guid experienceId, Guid classroomId, DateOnly scheduledDate, string timeOfDay, Guid? primaryTeacherId = null, TimeSpan? startTime = null, TimeSpan? endTime = null, string? notes = null)
    {
        if (!_userContext.HasPermission("experience.schedule")) throw new UnauthorizedAccessException("Requires experience.schedule permission.");

        // Basic validation
        if (startTime.HasValue && endTime.HasValue && endTime <= startTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }

        // Conflict Validation
        await ValidateScheduleConflictsAsync(classroomId, scheduledDate, timeOfDay, primaryTeacherId, startTime, endTime, excludeScheduleId: null);

        var schedule = new ExperienceSchedule
        {
            ExperienceId = experienceId,
            ClassroomId = classroomId,
            ScheduledDate = scheduledDate,
            TimeOfDay = timeOfDay,
            PrimaryTeacherId = primaryTeacherId,
            PlannedStartTime = startTime,
            PlannedEndTime = endTime,
            PreparationNotes = notes,
            Status = "Planned"
        };

        return await _scheduleRepository.CreateAsync(schedule);
    }

    public async Task UpdateScheduleAsync(ExperienceSchedule schedule)
    {
        if (!_userContext.HasPermission("experience.schedule")) throw new UnauthorizedAccessException("Requires experience.schedule permission.");
        if (schedule.PlannedStartTime.HasValue && schedule.PlannedEndTime.HasValue && schedule.PlannedEndTime <= schedule.PlannedStartTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }

        await ValidateScheduleConflictsAsync(
            schedule.ClassroomId, 
            schedule.ScheduledDate, 
            schedule.TimeOfDay, 
            schedule.PrimaryTeacherId, 
            schedule.PlannedStartTime, 
            schedule.PlannedEndTime, 
            excludeScheduleId: schedule.Id);

        await _scheduleRepository.UpdateAsync(schedule);
    }

    public async Task ConfirmScheduleAsync(Guid scheduleId)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule != null && schedule.Status == "Planned")
        {
            schedule.Status = "Confirmed";
            await _scheduleRepository.UpdateAsync(schedule);
        }
    }

    public async Task CompleteScheduleAsync(Guid scheduleId, string? notes = null)
    {
        if (!_userContext.HasPermission("experience.schedule") && !_userContext.HasPermission("experience.execute")) 
            throw new UnauthorizedAccessException("Requires experience.schedule or experience.execute permission.");

        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule != null)
        {
            schedule.Status = "Completed";
            if (!string.IsNullOrWhiteSpace(notes))
            {
                schedule.PreparationNotes = string.IsNullOrWhiteSpace(schedule.PreparationNotes)
                    ? $"Completed: {notes}"
                    : $"{schedule.PreparationNotes}\nCompleted: {notes}";
            }
            await _scheduleRepository.UpdateAsync(schedule);
        }
    }

    public async Task CancelScheduleAsync(Guid scheduleId)
    {
        if (!_userContext.HasPermission("experience.schedule")) throw new UnauthorizedAccessException("Requires experience.schedule permission.");
        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId);
        if (schedule != null)
        {
            schedule.Status = "Cancelled";
            await _scheduleRepository.UpdateAsync(schedule);
        }
    }

    public async Task DeleteScheduleAsync(Guid id)
    {
        if (!_userContext.HasPermission("experience.schedule")) throw new UnauthorizedAccessException("Requires experience.schedule permission.");
        await _scheduleRepository.DeleteAsync(id);
    }

    private async Task ValidateScheduleConflictsAsync(
        Guid classroomId,
        DateOnly scheduledDate,
        string timeOfDay,
        Guid? primaryTeacherId,
        TimeSpan? startTime,
        TimeSpan? endTime,
        Guid? excludeScheduleId = null)
    {
        var existingSchedules = await _scheduleRepository.GetByDateAsync(scheduledDate);
        foreach (var existing in existingSchedules)
        {
            if (excludeScheduleId.HasValue && existing.Id == excludeScheduleId.Value) continue;
            if (string.Equals(existing.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)) continue;

            bool timeOverlaps = false;
            if (startTime.HasValue && endTime.HasValue && existing.PlannedStartTime.HasValue && existing.PlannedEndTime.HasValue)
            {
                timeOverlaps = startTime < existing.PlannedEndTime && endTime > existing.PlannedStartTime;
            }
            else
            {
                // If explicit time range is not specified on both, match by TimeOfDay ("Morning", "Mid-Day", "Afternoon", etc.)
                timeOverlaps = string.IsNullOrWhiteSpace(timeOfDay) || 
                               string.IsNullOrWhiteSpace(existing.TimeOfDay) || 
                               string.Equals(timeOfDay.Trim(), existing.TimeOfDay.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            if (timeOverlaps)
            {
                if (existing.ClassroomId == classroomId)
                {
                    throw new InvalidOperationException($"Classroom and its students already have an experience scheduled for this time ({timeOfDay}). A classroom or student can be present in only one planned schedule event at a time.");
                }

                if (primaryTeacherId.HasValue && existing.PrimaryTeacherId.HasValue && existing.PrimaryTeacherId == primaryTeacherId)
                {
                    throw new InvalidOperationException($"Teacher is already assigned to another experience during this time ({timeOfDay}). A teacher can be present in only one planned schedule event at a time.");
                }
            }
        }
    }

    public async Task<TodayActivitiesFeedDto> GetTodayActivitiesFeedAsync(
        DateOnly date,
        Guid? branchId = null,
        Guid? classroomId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext?.TenantId ?? Guid.Empty;
        var userId = _userContext?.UserId ?? Guid.Empty;
        var userRoles = (_userContext?.Roles ?? Enumerable.Empty<string>()).ToList();
        bool isAdmin = userRoles.Any(r =>
            r.Equals("PlatformAdmin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("OrganizationAdmin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("OrgAdmin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("BranchAdmin", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase));

        // Assigned classrooms if user is educator
        var assignedClassroomIds = new HashSet<Guid>();
        if (!isAdmin && tenantId != Guid.Empty && userId != Guid.Empty && _accessScopeRepository != null)
        {
            var assigned = await _accessScopeRepository.GetAssignedClassroomIdsAsync(tenantId, userId);
            foreach (var cId in assigned) assignedClassroomIds.Add(cId);
        }

        // Teacher profiles & current educator assignment
        Teacher? currentProfile = null;
        var teacherProfiles = new List<Teacher>();
        if (_teacherRepository != null && tenantId != Guid.Empty)
        {
            var allTeachers = await _teacherRepository.GetAllAsync(tenantId, cancellationToken);
            teacherProfiles = allTeachers.ToList();
            currentProfile = teacherProfiles.FirstOrDefault(t => t.UserId == userId || t.Id == userId);
            if (currentProfile != null && !string.IsNullOrWhiteSpace(currentProfile.DraftData))
            {
                try
                {
                    using var doc = JsonDocument.Parse(currentProfile.DraftData);
                    if (doc.RootElement.TryGetProperty("AssignedClassroom", out var cToken) || doc.RootElement.TryGetProperty("assignedClassroom", out cToken))
                    {
                        if (Guid.TryParse(cToken.GetString(), out var cId)) assignedClassroomIds.Add(cId);
                    }
                }
                catch { }
            }
        }

        // Schedules for date
        var schedules = (await _scheduleRepository.GetByDateAsync(date)).ToList();

        // Filter by branch/classroom query params if specified
        if (classroomId.HasValue && classroomId.Value != Guid.Empty)
        {
            schedules = schedules.Where(s => s.ClassroomId == classroomId.Value).ToList();
        }

        // Filter by user role/assigned classrooms if not admin
        if (!isAdmin && assignedClassroomIds.Any())
        {
            schedules = schedules.Where(s =>
                assignedClassroomIds.Contains(s.ClassroomId) ||
                (currentProfile != null && s.PrimaryTeacherId == currentProfile.Id) ||
                s.PrimaryTeacherId == userId
            ).ToList();
        }

        // Classrooms lookup
        var classroomLookup = new Dictionary<Guid, Classroom>();
        if (_classroomRepository != null && tenantId != Guid.Empty)
        {
            var cls = await _classroomRepository.GetAllAsync(tenantId, cancellationToken);
            classroomLookup = cls.ToDictionary(c => c.Id);
        }

        // Branch lookup
        var branchLookup = new Dictionary<Guid, Branch>();
        if (_branchRepository != null && tenantId != Guid.Empty)
        {
            var uniqueBranchIds = classroomLookup.Values.Select(c => c.BranchId).Where(id => id != Guid.Empty).Distinct().ToList();
            foreach (var bId in uniqueBranchIds)
            {
                var br = await _branchRepository.GetByIdAsync(tenantId, bId, cancellationToken);
                if (br != null) branchLookup[br.Id] = br;
            }
        }

        if (branchId.HasValue && branchId.Value != Guid.Empty)
        {
            schedules = schedules.Where(s =>
            {
                if (classroomLookup.TryGetValue(s.ClassroomId, out var cl))
                {
                    return cl.BranchId == branchId.Value;
                }
                return false;
            }).ToList();
        }

        // Experiences lookup
        var experienceLookup = new Dictionary<Guid, Experience>();
        if (_experienceRepository != null && tenantId != Guid.Empty)
        {
            var exps = await _experienceRepository.GetAllAsync(tenantId, cancellationToken);
            experienceLookup = exps.ToDictionary(e => e.Id);
        }

        var items = new List<TodayActivityItemDto>();
        foreach (var s in schedules)
        {
            experienceLookup.TryGetValue(s.ExperienceId, out var exp);
            classroomLookup.TryGetValue(s.ClassroomId, out var cls);
            string branchName = "";
            if (cls != null && branchLookup.TryGetValue(cls.BranchId, out var br))
            {
                branchName = br.Name;
            }

            string teacherName = "";
            if (s.PrimaryTeacherId.HasValue)
            {
                var tProf = teacherProfiles.FirstOrDefault(t => t.Id == s.PrimaryTeacherId.Value || t.UserId == s.PrimaryTeacherId.Value);
                teacherName = tProf?.FullName ?? "";
            }

            string domain = "";
            string ageGroup = cls?.AgeGroup ?? "";
            if (exp != null && !string.IsNullOrWhiteSpace(exp.DnaPayload))
            {
                try
                {
                    using var dnaDoc = JsonDocument.Parse(exp.DnaPayload);
                    if (dnaDoc.RootElement.TryGetProperty("Domain", out var dToken) || dnaDoc.RootElement.TryGetProperty("domain", out dToken))
                    {
                        domain = dToken.GetString() ?? "";
                    }
                    if (string.IsNullOrEmpty(ageGroup) && (dnaDoc.RootElement.TryGetProperty("AgeGroup", out var agToken) || dnaDoc.RootElement.TryGetProperty("ageGroup", out agToken)))
                    {
                        ageGroup = agToken.GetString() ?? "";
                    }
                }
                catch { }
            }

            string timeRange;
            if (s.PlannedStartTime.HasValue && s.PlannedEndTime.HasValue)
            {
                var startDt = DateTime.Today.Add(s.PlannedStartTime.Value);
                var endDt = DateTime.Today.Add(s.PlannedEndTime.Value);
                timeRange = $"{startDt:hh:mm tt} – {endDt:hh:mm tt}";
            }
            else if (s.PlannedStartTime.HasValue)
            {
                var startDt = DateTime.Today.Add(s.PlannedStartTime.Value);
                timeRange = $"{startDt:hh:mm tt}";
            }
            else
            {
                timeRange = s.TimeOfDay?.ToLowerInvariant() switch
                {
                    "morning" => "09:00 AM – 10:30 AM",
                    "mid-day" => "11:00 AM – 12:30 PM",
                    "afternoon" => "02:00 PM – 03:30 PM",
                    _ => s.TimeOfDay ?? ""
                };
            }

            items.Add(new TodayActivityItemDto
            {
                ScheduleId = s.Id,
                ExperienceId = s.ExperienceId,
                ClassroomId = s.ClassroomId,
                ClassroomName = cls?.Name ?? "Classroom",
                BranchName = branchName,
                Title = exp?.Title ?? "Assigned Experience",
                Description = exp?.Description ?? "",
                TimeOfDay = s.TimeOfDay ?? "",
                PlannedStartTime = s.PlannedStartTime,
                PlannedEndTime = s.PlannedEndTime,
                TimeRangeDisplay = timeRange,
                Status = s.Status,
                PreparationNotes = s.PreparationNotes,
                Domain = domain,
                AgeGroup = ageGroup,
                TeacherName = teacherName,
                PrimaryTeacherId = s.PrimaryTeacherId
            });
        }

        var sorted = items.OrderBy(a => a.GetSortOrderTime()).ToList();

        return new TodayActivitiesFeedDto
        {
            Date = date,
            TotalScheduled = sorted.Count,
            InProgressCount = sorted.Count(a => a.IsRunning),
            CompletedCount = sorted.Count(a => a.IsCompleted),
            UpcomingCount = sorted.Count(a => a.IsPending),
            Activities = sorted
        };
    }
}

