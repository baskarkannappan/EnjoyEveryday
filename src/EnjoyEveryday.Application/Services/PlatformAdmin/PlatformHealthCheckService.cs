using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Shared.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Application.Services.PlatformAdmin;

public class PlatformHealthCheckService : IPlatformHealthCheckService
{
    private readonly IDbConnectionFactory? _connectionFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISystemConfigurationService? _systemConfig;
    private readonly ILogger<PlatformHealthCheckService>? _logger;

    public PlatformHealthCheckService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IDbConnectionFactory? connectionFactory = null,
        ISystemConfigurationService? systemConfig = null,
        ILogger<PlatformHealthCheckService>? logger = null)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _connectionFactory = connectionFactory;
        _systemConfig = systemConfig;
        _logger = logger;
    }

    public async Task<SystemHealthReport> RunChecksAsync(HealthCheckOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new HealthCheckOptions();
        var report = new SystemHealthReport
        {
            CheckedAt = DateTimeOffset.UtcNow
        };

        var overallSw = Stopwatch.StartNew();
        var tasks = new List<Task<ComponentHealthItem>>();

        if (options.CheckDatabase)
        {
            tasks.Add(CheckDatabaseAsync(options, cancellationToken));
        }
        else
        {
            report.Components.Add(CreateSkipped("database", "PostgreSQL Database", "Database", GetDatabaseEndpointDisplay()));
        }

        if (options.CheckApi)
        {
            tasks.Add(CheckApiAsync(options, cancellationToken));
        }
        else
        {
            report.Components.Add(CreateSkipped("api", "Backend REST API", "Core Services", GetApiBaseUrl(options)));
        }

        if (options.CheckAdminUi)
        {
            tasks.Add(CheckAdminUiAsync(cancellationToken));
        }
        else
        {
            report.Components.Add(CreateSkipped("admin-ui", "Platform Admin UI", "Web Portals", "In-Process"));
        }

        if (options.CheckDaycareUi)
        {
            tasks.Add(CheckDaycareUiAsync(options, cancellationToken));
        }
        else
        {
            report.Components.Add(CreateSkipped("daycare-ui", "Daycare Web Portal", "Web Portals", GetDaycareBaseUrl(options)));
        }

        if (options.CheckAiGateway)
        {
            tasks.Add(CheckAiGatewayAsync(options, cancellationToken));
        }
        else
        {
            report.Components.Add(CreateSkipped("ai-agent", "AI Gateway Agent", "AI & Intelligence", GetAiBaseUrl(options)));
        }

        var results = await Task.WhenAll(tasks);
        report.Components.AddRange(results);

        overallSw.Stop();
        report.TotalDurationMs = Math.Round(overallSw.Elapsed.TotalMilliseconds, 1);

        // Compute overall status
        if (report.Components.Any(c => c.Status == HealthStatusLevel.Unhealthy && (c.Id == "database" || c.Id == "api")))
        {
            report.OverallStatus = HealthStatusLevel.Unhealthy;
        }
        else if (report.Components.Any(c => c.Status == HealthStatusLevel.Unhealthy || c.Status == HealthStatusLevel.Degraded))
        {
            report.OverallStatus = HealthStatusLevel.Degraded;
        }
        else
        {
            report.OverallStatus = HealthStatusLevel.Healthy;
        }

        return report;
    }

    private async Task<ComponentHealthItem> CheckDatabaseAsync(HealthCheckOptions options, CancellationToken cancellationToken)
    {
        var item = new ComponentHealthItem
        {
            Id = "database",
            Name = "PostgreSQL Database",
            Category = "Database",
            Endpoint = GetDatabaseEndpointDisplay(),
            CheckedAt = DateTimeOffset.UtcNow
        };

        if (_connectionFactory == null)
        {
            item.Status = HealthStatusLevel.Degraded;
            item.Details = "Database connection factory is not registered in this host context.";
            return item;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(options.TimeoutSeconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            using var connection = await _connectionFactory.CreateConnectionAsync(linked.Token);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            var scalar = command.ExecuteScalar();

            sw.Stop();
            item.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);

            if (scalar != null && (scalar.ToString() == "1" || Convert.ToInt32(scalar) == 1))
            {
                item.Status = HealthStatusLevel.Healthy;
                item.Details = $"Connection responsive (SELECT 1 returned 1) • Database: {connection.Database ?? "enjoyeveryday"}";
            }
            else
            {
                item.Status = HealthStatusLevel.Degraded;
                item.Details = $"Unexpected scalar result from database: '{scalar}'";
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            item.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
            item.Status = HealthStatusLevel.Unhealthy;
            item.ErrorMessage = ex.Message;
            item.Details = "PostgreSQL database connection or query execution failed.";
            _logger?.LogWarning(ex, "Health check failed for Database.");
        }

        return item;
    }

    private async Task<ComponentHealthItem> CheckApiAsync(HealthCheckOptions options, CancellationToken cancellationToken)
    {
        var baseUrl = GetApiBaseUrl(options);
        var item = new ComponentHealthItem
        {
            Id = "api",
            Name = "Backend REST API",
            Category = "Core Services",
            Endpoint = baseUrl,
            CheckedAt = DateTimeOffset.UtcNow
        };

        var sw = Stopwatch.StartNew();
        try
        {
            using var client = CreateResilientHttpClient(options.TimeoutSeconds);
            var targetUri = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "healthz");

            using var response = await client.GetAsync(targetUri, cancellationToken);
            sw.Stop();
            item.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);

            if (response.IsSuccessStatusCode)
            {
                var body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
                item.Status = HealthStatusLevel.Healthy;
                item.Details = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} • Response: '{body}'";
            }
            else
            {
                item.Status = HealthStatusLevel.Degraded;
                item.Details = $"API returned non-success HTTP status {(int)response.StatusCode} {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            item.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
            item.Status = HealthStatusLevel.Unhealthy;
            item.ErrorMessage = ex.Message;
            item.Details = $"Unable to reach REST API service at {baseUrl}";
            _logger?.LogWarning(ex, "Health check failed for REST API at {BaseUrl}", baseUrl);
        }

        return item;
    }

    private Task<ComponentHealthItem> CheckAdminUiAsync(CancellationToken cancellationToken)
    {
        var item = new ComponentHealthItem
        {
            Id = "admin-ui",
            Name = "Platform Admin Web UI",
            Category = "Web Portals",
            Endpoint = "In-Process (Blazor Server)",
            CheckedAt = DateTimeOffset.UtcNow,
            LatencyMs = 0.1,
            Status = HealthStatusLevel.Healthy
        };

        var allocatedMb = Math.Round(GC.GetTotalMemory(forceFullCollection: false) / (1024.0 * 1024.0), 1);
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
        var uptimeStr = uptime.TotalHours >= 1 
            ? $"{(int)uptime.TotalHours}h {uptime.Minutes}m" 
            : $"{uptime.Minutes}m {uptime.Seconds}s";

        item.Details = $"Blazor Server Circuit active • Heap: {allocatedMb} MB • Uptime: {uptimeStr} • .NET {Environment.Version}";
        return Task.FromResult(item);
    }

    private async Task<ComponentHealthItem> CheckDaycareUiAsync(HealthCheckOptions options, CancellationToken cancellationToken)
    {
        var baseUrl = GetDaycareBaseUrl(options);
        var item = new ComponentHealthItem
        {
            Id = "daycare-ui",
            Name = "Daycare Web Portal",
            Category = "Web Portals",
            Endpoint = baseUrl,
            CheckedAt = DateTimeOffset.UtcNow
        };

        var sw = Stopwatch.StartNew();
        try
        {
            using var client = CreateResilientHttpClient(options.TimeoutSeconds);
            var targetUri = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "healthz");

            using var response = await client.GetAsync(targetUri, cancellationToken);
            sw.Stop();
            item.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);

            if (response.IsSuccessStatusCode)
            {
                item.Status = HealthStatusLevel.Healthy;
                item.Details = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} • Daycare portal operational";
            }
            else
            {
                item.Status = HealthStatusLevel.Degraded;
                item.Details = $"Daycare portal returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            item.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
            item.Status = HealthStatusLevel.Degraded;
            item.ErrorMessage = ex.Message;
            item.Details = $"Daycare portal endpoint unresponsive at {baseUrl}";
        }

        return item;
    }

    private async Task<ComponentHealthItem> CheckAiGatewayAsync(HealthCheckOptions options, CancellationToken cancellationToken)
    {
        var baseUrl = GetAiBaseUrl(options);
        var item = new ComponentHealthItem
        {
            Id = "ai-agent",
            Name = "AI Gateway Agent",
            Category = "AI & Intelligence",
            Endpoint = baseUrl,
            CheckedAt = DateTimeOffset.UtcNow
        };

        var sw = Stopwatch.StartNew();
        try
        {
            using var client = CreateResilientHttpClient(options.TimeoutSeconds);
            var targetUri = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "health");

            using var response = await client.GetAsync(targetUri, cancellationToken);
            sw.Stop();
            item.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);

            if (response.IsSuccessStatusCode)
            {
                item.Status = HealthStatusLevel.Healthy;
                item.Details = $"HTTP {(int)response.StatusCode} • FastAPI agent online";
            }
            else
            {
                item.Status = HealthStatusLevel.Degraded;
                item.Details = $"AI Agent returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            item.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
            // According to EnjoyEveryday Rule 7.4 (Deterministic Fallback), AI is an optional auxiliary service
            item.Status = HealthStatusLevel.Degraded;
            item.ErrorMessage = ex.Message;
            item.Details = "AI Gateway offline. Deterministic non-AI fallback rules active in application.";
        }

        return item;
    }

    private HttpClient CreateResilientHttpClient(int timeoutSeconds)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                // Accept self-signed dev certificates for local loopback development
                if (message.RequestUri?.IsLoopback == true)
                    return true;
                return errors == System.Net.Security.SslPolicyErrors.None;
            }
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
    }

    private string GetApiBaseUrl(HealthCheckOptions options) =>
        options.CustomApiUrl 
        ?? _systemConfig?.GetConfiguration().ApiUrl 
        ?? _configuration["Services:ApiUrl"] 
        ?? "https://localhost:7013";

    private string GetDaycareBaseUrl(HealthCheckOptions options) =>
        options.CustomDaycareUrl 
        ?? _systemConfig?.GetConfiguration().DaycareUrl 
        ?? _configuration["Services:DaycareUrl"] 
        ?? "https://localhost:7003";

    private string GetAiBaseUrl(HealthCheckOptions options) =>
        options.CustomAiUrl 
        ?? _systemConfig?.GetConfiguration().AiAgentUrl 
        ?? _configuration["AiAgent:BaseUrl"] 
        ?? "http://127.0.0.1:8000";

    private string GetDatabaseEndpointDisplay()
    {
        if (_systemConfig != null)
        {
            var cfg = _systemConfig.GetConfiguration();
            if (!string.IsNullOrWhiteSpace(cfg.DbHost))
            {
                return $"{cfg.DbHost}:{cfg.DbPort} / {cfg.DbName}";
            }
        }

        var connStr = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connStr)) return "Host unconfigured";

        try
        {
            var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = connStr };
            var host = builder.TryGetValue("Host", out var h) || builder.TryGetValue("Server", out h) ? h : "localhost";
            var port = builder.TryGetValue("Port", out var p) ? p : "5432";
            var db = builder.TryGetValue("Database", out var d) ? d : "enjoyeveryday";
            return $"{host}:{port} / {db}";
        }
        catch
        {
            return "Custom PostgreSQL Connection";
        }
    }

    private static ComponentHealthItem CreateSkipped(string id, string name, string category, string endpoint) =>
        new()
        {
            Id = id,
            Name = name,
            Category = category,
            Endpoint = endpoint,
            Status = HealthStatusLevel.Skipped,
            LatencyMs = 0,
            Details = "Check skipped based on user configuration.",
            CheckedAt = DateTimeOffset.UtcNow
        };
}
