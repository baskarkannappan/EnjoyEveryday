using System;

namespace EnjoyEveryday.Application.Services.PlatformAdmin;

public class SystemDeploymentConfig
{
    // Service Endpoints (Deployment URLs)
    public string ApiUrl { get; set; } = "https://localhost:7013";
    public string DaycareUrl { get; set; } = "https://localhost:7003";
    public string ParentUrl { get; set; } = "https://localhost:7005";
    public string AiAgentUrl { get; set; } = "http://127.0.0.1:8000";

    // Database Connection Parameters
    public string DbHost { get; set; } = "localhost";
    public int DbPort { get; set; } = 5433;
    public string DbName { get; set; } = "enjoyeveryday";
    public string DbUser { get; set; } = "postgres";
    public string DbPassword { get; set; } = "postgres";
    public string DbSslMode { get; set; } = "Disable";
    public int DbMaxPoolSize { get; set; } = 100;
    public string? RawConnectionString { get; set; }

    // Health Check & Diagnostics Policies
    public bool AutoCheckOnLanding { get; set; } = true;
    public int DefaultRefreshIntervalSeconds { get; set; } = 0; // 0 = Manual
    public int ProbeTimeoutSeconds { get; set; } = 5;
    public bool DefaultCheckDatabase { get; set; } = true;
    public bool DefaultCheckApi { get; set; } = true;
    public bool DefaultCheckAdminUi { get; set; } = true;
    public bool DefaultCheckDaycareUi { get; set; } = true;
    public bool DefaultCheckAiGateway { get; set; } = true;

    // Environmental Metadata
    public string EnvironmentName { get; set; } = "Development";
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public double LatencyMs { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset TestedAt { get; set; } = DateTimeOffset.UtcNow;
}
