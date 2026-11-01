using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace EnjoyEveryday.Api.Middleware;

/// <summary>
/// Microsoft native IExceptionHandler (RFC 7807 ProblemDetails).
/// Provides centralized, resilient, and safe error responses across the entire API.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var (statusCode, title, detail) = MapException(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled server error processing HTTP {Method} {Path} [TraceId: {TraceId}]",
                httpContext.Request.Method, httpContext.Request.Path, traceId);
        }
        else
        {
            _logger.LogWarning("Handled client error HTTP {StatusCode} processing {Method} {Path} [TraceId: {TraceId}]: {Message}",
                statusCode, httpContext.Request.Method, httpContext.Request.Path, traceId, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = _environment.IsDevelopment() ? $"{detail} ({exception.Message})" : detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException argNull => (
                StatusCodes.Status400BadRequest,
                "Invalid Parameter",
                $"Required parameter '{argNull.ParamName}' was null or empty."
            ),
            ArgumentException argEx => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                argEx.Message
            ),
            KeyNotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                notFound.Message
            ),
            UnauthorizedAccessException unauthorized => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to perform this action."
            ),
            InvalidOperationException invOp => (
                StatusCodes.Status409Conflict,
                "Conflict",
                invOp.Message
            ),
            PostgresException pgEx => MapPostgresException(pgEx),
            TimeoutException => (
                StatusCodes.Status504GatewayTimeout,
                "Operation Timeout",
                "The database or backend service timed out while processing your request. Please try again."
            ),
            OperationCanceledException => (
                499, // Client Closed Request
                "Request Cancelled",
                "The operation was cancelled by the client or timed out."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred while processing your request. Please contact support with the trace ID."
            )
        };
    }

    private static (int StatusCode, string Title, string Detail) MapPostgresException(PostgresException pgEx)
    {
        return pgEx.SqlState switch
        {
            // Unique violation (e.g. duplicate email, unique code)
            PostgresErrorCodes.UniqueViolation => (
                StatusCodes.Status409Conflict,
                "Duplicate Record",
                "A record with this identifier or unique attribute already exists."
            ),
            // Foreign key violation
            PostgresErrorCodes.ForeignKeyViolation => (
                StatusCodes.Status400BadRequest,
                "Foreign Key Constraint",
                "Referenced entity does not exist or has active dependent records."
            ),
            // Check violation
            PostgresErrorCodes.CheckViolation => (
                StatusCodes.Status400BadRequest,
                "Constraint Violation",
                "The requested change violates a data integrity rule."
            ),
            // Connection/Admin shutdown
            PostgresErrorCodes.AdminShutdown or PostgresErrorCodes.CrashShutdown => (
                StatusCodes.Status503ServiceUnavailable,
                "Database Unavailable",
                "The database is currently undergoing maintenance or restart. Please retry shortly."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Database Error",
                "A database operation failed while executing the request."
            )
        };
    }
}
