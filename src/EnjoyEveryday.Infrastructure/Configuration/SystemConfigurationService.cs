using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services.PlatformAdmin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EnjoyEveryday.Infrastructure.Configuration;

public class SystemConfigurationService : ISystemConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SystemConfigurationService>? _logger;
    private readonly object _lock = new();

    private SystemDeploymentConfig _currentConfig;

    public SystemConfigurationService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SystemConfigurationService>? logger = null)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _currentConfig = LoadInitialConfiguration();
    }

    public SystemDeploymentConfig GetConfiguration()
    {
        lock (_lock)
        {
            // Return a shallow copy so modifications don't mutate state outside UpdateConfigurationAsync
            return new SystemDeploymentConfig
            {
                ApiUrl = _currentConfig.ApiUrl,
                DaycareUrl = _currentConfig.DaycareUrl,
                ParentUrl = _currentConfig.ParentUrl,
                AiAgentUrl = _currentConfig.AiAgentUrl,

                DbHost = _currentConfig.DbHost,
                DbPort = _currentConfig.DbPort,
                DbName = _currentConfig.DbName,
                DbUser = _currentConfig.DbUser,
                DbPassword = _currentConfig.DbPassword,
                DbSslMode = _currentConfig.DbSslMode,
                DbMaxPoolSize = _currentConfig.DbMaxPoolSize,
                RawConnectionString = _currentConfig.RawConnectionString,

                AutoCheckOnLanding = _currentConfig.AutoCheckOnLanding,
                DefaultRefreshIntervalSeconds = _currentConfig.DefaultRefreshIntervalSeconds,
                ProbeTimeoutSeconds = _currentConfig.ProbeTimeoutSeconds,
                DefaultCheckDatabase = _currentConfig.DefaultCheckDatabase,
                DefaultCheckApi = _currentConfig.DefaultCheckApi,
                DefaultCheckAdminUi = _currentConfig.DefaultCheckAdminUi,
                DefaultCheckDaycareUi = _currentConfig.DefaultCheckDaycareUi,
                DefaultCheckAiGateway = _currentConfig.DefaultCheckAiGateway,

                EnvironmentName = _currentConfig.EnvironmentName,
                LastUpdatedAt = _currentConfig.LastUpdatedAt
            };
        }
    }

    public Task UpdateConfigurationAsync(SystemDeploymentConfig newConfig, CancellationToken ct = default)
    {
        lock (_lock)
        {
            newConfig.LastUpdatedAt = DateTimeOffset.UtcNow;
            _currentConfig = newConfig;
            _logger?.LogInformation("System deployment configuration updated dynamically by Platform Admin at {Timestamp}", newConfig.LastUpdatedAt);
        }

        return Task.CompletedTask;
    }

    public async Task<ConnectionTestResult> TestDatabaseConnectionAsync(
        string? connectionString = null, 
        int timeoutSeconds = 5, 
        CancellationToken ct = default)
    {
        var connStr = !string.IsNullOrWhiteSpace(connectionString) 
            ? connectionString 
            : BuildConnectionString(GetConfiguration());

        var sw = Stopwatch.StartNew();
        var result = new ConnectionTestResult { TestedAt = DateTimeOffset.UtcNow };

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(linked.Token);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1;";
            var scalar = await cmd.ExecuteScalarAsync(linked.Token);

            sw.Stop();
            result.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);

            if (scalar != null && (scalar.ToString() == "1" || Convert.ToInt32(scalar) == 1))
            {
                result.Success = true;
                result.Message = $"PostgreSQL connected successfully • Host: {conn.Host}:{conn.Port} • DB: {conn.Database} • Server Version: {conn.PostgreSqlVersion}";
            }
            else
            {
                result.Success = false;
                result.Message = $"Query executed but returned unexpected scalar: '{scalar}'";
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Message = "Failed to establish PostgreSQL connection.";
            _logger?.LogWarning(ex, "TestDatabaseConnectionAsync failed for connection string.");
        }

        return result;
    }

    public async Task<ConnectionTestResult> TestHttpEndpointAsync(
        string url, 
        string relativePath = "healthz", 
        int timeoutSeconds = 5, 
        CancellationToken ct = default)
    {
        var result = new ConnectionTestResult { TestedAt = DateTimeOffset.UtcNow };

        if (string.IsNullOrWhiteSpace(url))
        {
            result.Success = false;
            result.Message = "Endpoint URL cannot be empty.";
            return result;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            using var client = CreateResilientHttpClient(timeoutSeconds);

            var baseUri = new Uri(url.TrimEnd('/') + "/");
            var targetUri = !string.IsNullOrWhiteSpace(relativePath) ? new Uri(baseUri, relativePath.TrimStart('/')) : baseUri;

            using var response = await client.GetAsync(targetUri, ct);
            sw.Stop();
            result.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);

            if (response.IsSuccessStatusCode)
            {
                var body = (await response.Content.ReadAsStringAsync(ct)).Trim();
                result.Success = true;
                result.Message = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} • Payload: '{body}'";
            }
            else
            {
                result.Success = false;
                result.Message = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                result.ErrorMessage = $"Server returned non-success HTTP status code {(int)response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Message = $"Unable to reach service at {url}";
        }

        return result;
    }

    public string BuildConnectionString(SystemDeploymentConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.RawConnectionString))
        {
            return config.RawConnectionString;
        }

        try
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = config.DbHost,
                Port = config.DbPort > 0 ? config.DbPort : 5432,
                Database = config.DbName,
                Username = config.DbUser,
                Password = config.DbPassword,
                MaxPoolSize = config.DbMaxPoolSize > 0 ? config.DbMaxPoolSize : 100
            };

            if (Enum.TryParse<SslMode>(config.DbSslMode, true, out var ssl))
            {
                builder.SslMode = ssl;
            }

            return builder.ConnectionString;
        }
        catch
        {
            return $"Host={config.DbHost};Port={config.DbPort};Database={config.DbName};Username={config.DbUser};Password={config.DbPassword}";
        }
    }

    private SystemDeploymentConfig LoadInitialConfiguration()
    {
        var config = new SystemDeploymentConfig
        {
            ApiUrl = _configuration["Services:ApiUrl"] ?? "https://localhost:7013",
            DaycareUrl = _configuration["Services:DaycareUrl"] ?? "https://localhost:7003",
            ParentUrl = _configuration["Services:ParentUrl"] ?? "https://localhost:7005",
            AiAgentUrl = _configuration["AiAgent:BaseUrl"] ?? "http://127.0.0.1:8000",
            EnvironmentName = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var rawConn = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(rawConn))
        {
            try
            {
                var npgsqlBuilder = new NpgsqlConnectionStringBuilder(rawConn);
                config.DbHost = npgsqlBuilder.Host ?? "localhost";
                config.DbPort = npgsqlBuilder.Port;
                config.DbName = npgsqlBuilder.Database ?? "enjoyeveryday";
                config.DbUser = npgsqlBuilder.Username ?? "postgres";
                config.DbPassword = npgsqlBuilder.Password ?? "postgres";
                config.DbSslMode = npgsqlBuilder.SslMode.ToString();
                config.DbMaxPoolSize = npgsqlBuilder.MaxPoolSize;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to parse initial Npgsql connection string; using defaults.");
                config.RawConnectionString = rawConn;
            }
        }

        return config;
    }

    private HttpClient CreateResilientHttpClient(int timeoutSeconds)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (message.RequestUri?.IsLoopback == true)
                    return true;
                return errors == System.Net.Security.SslPolicyErrors.None;
            }
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds))
        };
    }
}
