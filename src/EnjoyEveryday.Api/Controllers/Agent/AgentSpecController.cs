using System;
using System.Collections.Generic;
using System.Security.Claims;
using EnjoyEveryday.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Controllers.Agent;

public record AgentToolDefinition(
    string Type,
    AgentFunctionDefinition Function);

public record AgentFunctionDefinition(
    string Name,
    string Description,
    Dictionary<string, object> Parameters);

[ApiController]
[Route("api/v1/agent")]
[Authorize]
public class AgentSpecController : ControllerBase
{
    [HttpGet("tools.json")]
    public ActionResult GetAgentToolsSpec()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? RoleTemplates.OrganizationAdmin;
        var canonicalRole = RoleTemplates.Resolve(role);

        var tools = new List<AgentToolDefinition>();

        // Parent persona tools
        if (canonicalRole == RoleTemplates.Parent)
        {
            tools.Add(new AgentToolDefinition("function", new AgentFunctionDefinition(
                "get_child_care_summary",
                "Retrieves verified daily care records (meals, naps, bottles, diaper changes) for the authorized child.",
                new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["childId"] = new { type = "string", format = "uuid", description = "The UUID of the child." },
                        ["date"] = new { type = "string", format = "date", description = "Target date in YYYY-MM-DD format (defaults to today)." }
                    },
                    ["required"] = new[] { "childId" }
                }
            )));

            tools.Add(new AgentToolDefinition("function", new AgentFunctionDefinition(
                "send_educator_message",
                "Sends a direct communication message from the parent to the classroom teacher regarding the child.",
                new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["childId"] = new { type = "string", format = "uuid", description = "The UUID of the child." },
                        ["messageText"] = new { type = "string", description = "Message content to send to the classroom team." }
                    },
                    ["required"] = new[] { "childId", "messageText" }
                }
            )));
        }

        // Educator / Lead Teacher tools
        if (canonicalRole == RoleTemplates.ClassroomTeacher 
            || canonicalRole == RoleTemplates.DaycareTeacher 
            || canonicalRole == RoleTemplates.LeadTeacher 
            || canonicalRole == RoleTemplates.BranchAdmin 
            || canonicalRole == RoleTemplates.OrganizationAdmin)
        {
            tools.Add(new AgentToolDefinition("function", new AgentFunctionDefinition(
                "get_classroom_roster",
                "Returns the active classroom roster, headcounts, check-in status, and medical care plans.",
                new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["classroomId"] = new { type = "string", format = "uuid", description = "The UUID of the classroom." }
                    },
                    ["required"] = new[] { "classroomId" }
                }
            )));

            tools.Add(new AgentToolDefinition("function", new AgentFunctionDefinition(
                "log_daily_care_routine",
                "Records a care event (Meal, Bottle, Diaper change, Nap) for a child in the classroom.",
                new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["childId"] = new { type = "string", format = "uuid", description = "The UUID of the child." },
                        ["careType"] = new { type = "string", @enum = new[] { "Meal", "Bottle", "Diaper", "Nap" }, description = "Category of routine care." },
                        ["details"] = new { type = "string", description = "Specific details (e.g. 'Consumed 180ml formula', 'Slept 45 mins')." }
                    },
                    ["required"] = new[] { "childId", "careType", "details" }
                }
            )));

            tools.Add(new AgentToolDefinition("function", new AgentFunctionDefinition(
                "recommend_curriculum_experiences",
                "Searches the curriculum catalog and recommends age-appropriate Montessori and experiential activities.",
                new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["theme"] = new { type = "string", description = "Learning theme or topic (e.g. 'Autumn leaves', 'Water play', 'Shapes')." },
                        ["durationMinutes"] = new { type = "integer", description = "Target duration in minutes (e.g. 20, 30)." }
                    },
                    ["required"] = new[] { "theme" }
                }
            )));
        }

        // Branch & Org Admin tools
        if (canonicalRole == RoleTemplates.BranchAdmin || canonicalRole == RoleTemplates.OrganizationAdmin)
        {
            tools.Add(new AgentToolDefinition("function", new AgentFunctionDefinition(
                "get_staff_coverage_and_ratios",
                "Audits educator-to-child ratio compliance and active staff shift coverage across classrooms.",
                new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["branchId"] = new { type = "string", format = "uuid", description = "The UUID of the branch." },
                        ["date"] = new { type = "string", format = "date", description = "Target audit date (YYYY-MM-DD)." }
                    },
                    ["required"] = new[] { "branchId" }
                }
            )));

            tools.Add(new AgentToolDefinition("function", new AgentFunctionDefinition(
                "get_branch_daily_operations_summary",
                "Returns attendance rate, meal counts, absent children, and active classroom alerts for the branch.",
                new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["branchId"] = new { type = "string", format = "uuid", description = "The UUID of the branch." }
                    },
                    ["required"] = new[] { "branchId" }
                }
            )));
        }

        return Ok(new
        {
            specVersion = "1.0.0",
            protocol = "FunctionCalling/OpenAPI3.1",
            role = canonicalRole,
            toolsCount = tools.Count,
            tools = tools
        });
    }
}
