using System.Threading;
using System.Threading.Tasks;

namespace EnjoyEveryday.Application.Services.PlatformAdmin;

public interface IPlatformHealthCheckService
{
    Task<SystemHealthReport> RunChecksAsync(HealthCheckOptions? options = null, CancellationToken cancellationToken = default);
}
