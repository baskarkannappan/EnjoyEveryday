using System;
using System.Collections.Generic;
using System.Linq;

namespace EnjoyEveryday.Application.Services.PlatformAdmin;

public enum HealthStatusLevel
{
    Healthy,
    Degraded,
    Unhealthy,
    Skipped
}

public class ComponentHealthItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public HealthStatusLevel Status { get; set; } = HealthStatusLevel.Healthy;
    public double LatencyMs { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class HealthCheckOptions
{
    public bool CheckDatabase { get; set; } = true;
    public bool CheckApi { get; set; } = true;
    public bool CheckAdminUi { get; set; } = true;
    public bool CheckDaycareUi { get; set; } = true;
    public bool CheckAiGateway { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 5;
    public string? CustomApiUrl { get; set; }
    public string? CustomDaycareUrl { get; set; }
    public string? CustomAiUrl { get; set; }
}

public class SystemHealthReport
{
    public HealthStatusLevel OverallStatus { get; set; } = HealthStatusLevel.Healthy;
    public double TotalDurationMs { get; set; }
    public DateTimeOffset CheckedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ComponentHealthItem> Components { get; set; } = new();

    public int HealthyCount => Components.Count(c => c.Status == HealthStatusLevel.Healthy);
    public int DegradedCount => Components.Count(c => c.Status == HealthStatusLevel.Degraded);
    public int UnhealthyCount => Components.Count(c => c.Status == HealthStatusLevel.Unhealthy);
    public int SkippedCount => Components.Count(c => c.Status == HealthStatusLevel.Skipped);
}
