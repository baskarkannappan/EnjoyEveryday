using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnjoyEveryday.Api.Mcp;

/// <summary>
/// Model Context Protocol (MCP) Server Endpoint for EnjoyEveryday.
/// Implements standard JSON-RPC 2.0 specification for AI agent tool and resource discovery and execution.
/// </summary>
[ApiController]
[Route("mcp")]
[Authorize]
public class McpController : ControllerBase
{
    private readonly McpToolExecutor _toolExecutor;

    public McpController(McpToolExecutor toolExecutor)
    {
        _toolExecutor = toolExecutor ?? throw new ArgumentNullException(nameof(toolExecutor));
    }

    [HttpPost]
    public async Task<ActionResult<McpRpcResponse>> HandleRpcRequest(
        [FromBody] JsonElement rawBody,
        CancellationToken cancellationToken)
    {
        object? id = null;
        try
        {
            if (rawBody.ValueKind != JsonValueKind.Object)
            {
                return Ok(new McpRpcResponse
                {
                    Error = new McpRpcError { Code = -32600, Message = "Invalid Request: root must be a JSON object" }
                });
            }

            if (rawBody.TryGetProperty("id", out var idProp))
            {
                id = idProp.ValueKind == JsonValueKind.Number ? idProp.GetInt64() : idProp.GetString();
            }

            if (!rawBody.TryGetProperty("method", out var methodProp) || methodProp.GetString() == null)
            {
                return Ok(new McpRpcResponse
                {
                    Id = id,
                    Error = new McpRpcError { Code = -32600, Message = "Method name is required." }
                });
            }

            var method = methodProp.GetString()!;
            var role = User.FindFirstValue(ClaimTypes.Role) ?? RoleTemplates.OrganizationAdmin;
            var tenantStr = User.FindFirstValue("tenant_id") ?? "10000000-0000-0000-0000-000000000001";
            var tenantId = Guid.Parse(tenantStr);

            switch (method.ToLowerInvariant())
            {
                case "ping":
                    return Ok(new McpRpcResponse { Id = id, Result = new { } });

                case "tools/list":
                    var tools = GetAvailableToolsForRole(role);
                    return Ok(new McpRpcResponse
                    {
                        Id = id,
                        Result = new { tools }
                    });

                case "tools/call":
                    if (!rawBody.TryGetProperty("params", out var paramsProp))
                    {
                        return Ok(new McpRpcResponse
                        {
                            Id = id,
                            Error = new McpRpcError { Code = -32602, Message = "Missing 'params' in tool call." }
                        });
                    }

                    if (!paramsProp.TryGetProperty("name", out var toolNameProp))
                    {
                        return Ok(new McpRpcResponse
                        {
                            Id = id,
                            Error = new McpRpcError { Code = -32602, Message = "Missing 'name' in tool call parameters." }
                        });
                    }

                    var toolName = toolNameProp.GetString() ?? "";
                    var args = paramsProp.TryGetProperty("arguments", out var argsProp) ? argsProp : default;

                    var callResult = await _toolExecutor.ExecuteToolAsync(toolName, args, tenantId, role, cancellationToken);
                    return Ok(new McpRpcResponse
                    {
                        Id = id,
                        Result = callResult
                    });

                case "resources/list":
                    return Ok(new McpRpcResponse
                    {
                        Id = id,
                        Result = new
                        {
                            resources = new object[]
                            {
                                new { uri = "enjoyeveryday://classrooms", name = "Classrooms List", mimeType = "application/json" },
                                new { uri = "enjoyeveryday://experiences/catalog", name = "Curriculum Experiences Catalog", mimeType = "application/json" }
                            }
                        }
                    });

                default:
                    return Ok(new McpRpcResponse
                    {
                        Id = id,
                        Error = new McpRpcError { Code = -32601, Message = $"Method '{method}' not found." }
                    });
            }
        }
        catch (Exception ex)
        {
            return Ok(new McpRpcResponse
            {
                Id = id,
                Error = new McpRpcError { Code = -32000, Message = ex.Message }
            });
        }
    }

    [HttpGet("sse")]
    public async Task HandleSseSession(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var endpointUri = $"{Request.Scheme}://{Request.Host}/mcp";
        await Response.WriteAsync($"event: endpoint\ndata: {endpointUri}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        // Keep connection open until client closes or cancellation requested
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(15000, cancellationToken);
            await Response.WriteAsync("event: ping\ndata: {}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    private static List<McpTool> GetAvailableToolsForRole(string role)
    {
        var canonical = RoleTemplates.Resolve(role);
        var tools = new List<McpTool>();

        if (canonical == RoleTemplates.Parent)
        {
            tools.Add(new McpTool
            {
                Name = "get_child_care_summary",
                Description = "Retrieves verified daily care records (meals, naps, bottles, diaper changes) for the child.",
                InputSchema = new
                {
                    type = "object",
                    properties = new { childId = new { type = "string", format = "uuid" }, date = new { type = "string", format = "date" } },
                    required = new[] { "childId" }
                }
            });
            tools.Add(new McpTool
            {
                Name = "send_educator_message",
                Description = "Sends a message from parent to classroom educator.",
                InputSchema = new
                {
                    type = "object",
                    properties = new { childId = new { type = "string", format = "uuid" }, messageText = new { type = "string" } },
                    required = new[] { "childId", "messageText" }
                }
            });
        }

        if (canonical != RoleTemplates.Parent)
        {
            tools.Add(new McpTool
            {
                Name = "get_classroom_roster",
                Description = "Returns the active classroom roster, headcounts, check-in status, and medical care plans.",
                InputSchema = new
                {
                    type = "object",
                    properties = new { classroomId = new { type = "string", format = "uuid" } },
                    required = new[] { "classroomId" }
                }
            });
            tools.Add(new McpTool
            {
                Name = "recommend_curriculum_experiences",
                Description = "Searches the catalog and recommends Montessori and experiential activities.",
                InputSchema = new
                {
                    type = "object",
                    properties = new { theme = new { type = "string" }, durationMinutes = new { type = "integer" } },
                    required = new[] { "theme" }
                }
            });
        }

        if (canonical == RoleTemplates.BranchAdmin || canonical == RoleTemplates.OrganizationAdmin)
        {
            tools.Add(new McpTool
            {
                Name = "get_staff_coverage_and_ratios",
                Description = "Audits educator-to-child ratio compliance and active staff shift coverage.",
                InputSchema = new
                {
                    type = "object",
                    properties = new { branchId = new { type = "string", format = "uuid" }, date = new { type = "string", format = "date" } },
                    required = new[] { "branchId" }
                }
            });
            tools.Add(new McpTool
            {
                Name = "get_branch_daily_operations_summary",
                Description = "Returns attendance rate, meal counts, absent children, and active alerts for the branch.",
                InputSchema = new
                {
                    type = "object",
                    properties = new { branchId = new { type = "string", format = "uuid" } },
                    required = new[] { "branchId" }
                }
            });
        }

        return tools;
    }
}
