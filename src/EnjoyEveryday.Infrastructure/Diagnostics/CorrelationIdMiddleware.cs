using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Diagnostics;

/// <summary>
/// Ambient correlation ID middleware that extracts or initializes an X-Correlation-Id header,
/// applies it to response headers, and enriches structured logging scope.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string CorrelationHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? correlationId = null;

        if (context.Request.Headers.TryGetValue(CorrelationHeader, out var headerValues))
        {
            var candidate = headerValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= 64)
            {
                correlationId = candidate;
            }
        }

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        context.Items["CorrelationId"] = correlationId;
        context.TraceIdentifier = correlationId;

        // Ensure response header is populated before headers are flushed to client
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationHeader))
            {
                context.Response.Headers[CorrelationHeader] = correlationId;
            }
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
