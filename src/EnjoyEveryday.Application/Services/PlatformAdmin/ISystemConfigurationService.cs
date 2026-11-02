using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services.PlatformAdmin;

public interface ISystemConfigurationService
{
    SystemDeploymentConfig GetConfiguration();
    Task UpdateConfigurationAsync(SystemDeploymentConfig newConfig, CancellationToken ct = default);
    Task<ConnectionTestResult> TestDatabaseConnectionAsync(string? connectionString = null, int timeoutSeconds = 5, CancellationToken ct = default);
    Task<ConnectionTestResult> TestHttpEndpointAsync(string url, string relativePath = "healthz", int timeoutSeconds = 5, CancellationToken ct = default);
    string BuildConnectionString(SystemDeploymentConfig config);
}
