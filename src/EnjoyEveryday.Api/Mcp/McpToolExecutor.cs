using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using EnjoyEveryday.Domain.Repositories;

namespace EnjoyEveryday.Api.Mcp;

/// <summary>
/// Executes MCP tool requests using EnjoyEveryday application and domain services.
/// </summary>
public class McpToolExecutor
{
    private readonly IChildDailyCareService _dailyCareService;
    private readonly IClassroomCommunicationService _communicationService;
    private readonly ExperienceService _experienceService;
    private readonly IStaffRosterService _rosterService;
    private readonly IDaycareOperationsBoardService _operationsBoardService;
    private readonly IChildRepository _childRepo;
    private readonly IClassroomRepository _classroomRepo;

    public McpToolExecutor(
        IChildDailyCareService dailyCareService,
        IClassroomCommunicationService communicationService,
        ExperienceService experienceService,
        IStaffRosterService rosterService,
        IDaycareOperationsBoardService operationsBoardService,
        IChildRepository childRepo,
        IClassroomRepository classroomRepo)
    {
        _dailyCareService = dailyCareService;
        _communicationService = communicationService;
        _experienceService = experienceService;
        _rosterService = rosterService;
        _operationsBoardService = operationsBoardService;
        _childRepo = childRepo;
        _classroomRepo = classroomRepo;
    }

    public async Task<McpCallResult> ExecuteToolAsync(
        string toolName,
        JsonElement arguments,
        Guid tenantId,
        string role,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (toolName.ToLowerInvariant())
            {
                case "get_child_care_summary":
                {
                    if (!arguments.TryGetProperty("childId", out var childIdProp) || !Guid.TryParse(childIdProp.GetString(), out var childId))
                    {
                        return CreateErrorResult("Argument 'childId' (UUID) is required.");
                    }

                    var child = await _childRepo.GetByIdAsync(tenantId, childId, cancellationToken);
                    if (child == null)
                    {
                        return CreateErrorResult($"Child with ID {childId} was not found.");
                    }

                    var date = DateOnly.FromDateTime(DateTime.Today);
                    if (arguments.TryGetProperty("date", out var dateProp) && DateOnly.TryParse(dateProp.GetString(), out var parsedDate))
                    {
                        date = parsedDate;
                    }

                    var package = await _dailyCareService.GetChildCarePackageAsync(childId, date, cancellationToken);

                    return CreateSuccessResult(new
                    {
                        childId = child.Id,
                        childName = child.FullName,
                        date = date.ToString("yyyy-MM-dd"),
                        timelineEventsCount = package?.ChildTimeline?.Count ?? 0,
                        events = package?.ChildTimeline ?? new()
                    });
                }

                case "send_educator_message":
                {
                    if (!arguments.TryGetProperty("childId", out var childIdProp) || !Guid.TryParse(childIdProp.GetString(), out var childId))
                    {
                        return CreateErrorResult("Argument 'childId' (UUID) is required.");
                    }
                    if (!arguments.TryGetProperty("messageText", out var textProp) || string.IsNullOrWhiteSpace(textProp.GetString()))
                    {
                        return CreateErrorResult("Argument 'messageText' is required.");
                    }

                    var sent = await _communicationService.SendEducatorMessageAsync(
                        childId,
                        textProp.GetString()!,
                        null,
                        cancellationToken);

                    return CreateSuccessResult(new
                    {
                        status = "Sent",
                        messageId = sent.Id,
                        sender = sent.SenderName,
                        timestamp = sent.Timestamp,
                        text = sent.Content
                    });
                }

                case "get_classroom_roster":
                {
                    if (!arguments.TryGetProperty("classroomId", out var classIdProp) || !Guid.TryParse(classIdProp.GetString(), out var classroomId))
                    {
                        return CreateErrorResult("Argument 'classroomId' (UUID) is required.");
                    }

                    var classroom = await _classroomRepo.GetByIdAsync(tenantId, classroomId, cancellationToken);
                    if (classroom == null)
                    {
                        return CreateErrorResult($"Classroom {classroomId} was not found.");
                    }

                    var children = (await _childRepo.GetByClassroomIdAsync(tenantId, classroomId, cancellationToken)).ToList();

                    return CreateSuccessResult(new
                    {
                        classroomId = classroom.Id,
                        classroomName = classroom.Name,
                        capacity = classroom.Capacity,
                        enrolledCount = children.Count,
                        roster = children.ConvertAll(c => new
                        {
                            id = c.Id,
                            name = c.FullName,
                            dateOfBirth = c.DateOfBirth?.ToString("yyyy-MM-dd"),
                            enrollmentStatus = c.EnrollmentStatus,
                            isActive = c.IsActive
                        })
                    });
                }

                case "recommend_curriculum_experiences":
                {
                    var theme = arguments.TryGetProperty("theme", out var tProp) ? tProp.GetString() ?? "Creative Exploration" : "Creative Exploration";
                    var catalog = (await _experienceService.GetCatalogAsync(cancellationToken: cancellationToken)).ToList();

                    return CreateSuccessResult(new
                    {
                        requestedTheme = theme,
                        catalogCount = catalog.Count,
                        recommendations = catalog.Take(5).Select(c => new
                        {
                            id = c.Id,
                            title = c.Title,
                            description = c.Description,
                            type = c.ExperienceType,
                            duration = c.Duration,
                            environment = c.Environment
                        })
                    });
                }

                case "get_staff_coverage_and_ratios":
                {
                    if (!arguments.TryGetProperty("branchId", out var branchIdProp) || !Guid.TryParse(branchIdProp.GetString(), out var branchId))
                    {
                        return CreateErrorResult("Argument 'branchId' (UUID) is required.");
                    }

                    var date = arguments.TryGetProperty("date", out var dProp) && DateTime.TryParse(dProp.GetString(), out var parsedDate)
                        ? parsedDate
                        : DateTime.Today;

                    var overview = await _rosterService.GetRosterOverviewAsync(branchId, date, cancellationToken);

                    return CreateSuccessResult(new
                    {
                        branchId = branchId,
                        date = date.ToString("yyyy-MM-dd"),
                        overview = overview
                    });
                }

                case "get_branch_daily_operations_summary":
                {
                    if (!arguments.TryGetProperty("branchId", out var branchIdProp) || !Guid.TryParse(branchIdProp.GetString(), out var branchId))
                    {
                        return CreateErrorResult("Argument 'branchId' (UUID) is required.");
                    }

                    var board = await _operationsBoardService.GetOperationsBoardAsync(new OperationsBoardFilter
                    {
                        BranchId = branchId,
                        Date = DateTime.Today
                    }, cancellationToken);

                    return CreateSuccessResult(new
                    {
                        branchId = branchId,
                        date = DateTime.Today.ToString("yyyy-MM-dd"),
                        expectedChildren = board.ExpectedChildrenCount,
                        checkedInChildren = board.CheckedInChildrenCount,
                        presentStaff = board.PresentStaffCount,
                        missingCareRecords = board.MissingCareRecordsCount,
                        alertsCount = board.Alerts.Count
                    });
                }

                default:
                    return CreateErrorResult($"Tool '{toolName}' is not recognized or not available for your role.");
            }
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Execution failed: {ex.Message}");
        }
    }

    private static McpCallResult CreateSuccessResult(object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        return new McpCallResult
        {
            IsError = false,
            Content = new List<McpContentItem>
            {
                new() { Type = "text", Text = json }
            }
        };
    }

    private static McpCallResult CreateErrorResult(string message)
    {
        return new McpCallResult
        {
            IsError = true,
            Content = new List<McpContentItem>
            {
                new() { Type = "text", Text = message }
            }
        };
    }
}
