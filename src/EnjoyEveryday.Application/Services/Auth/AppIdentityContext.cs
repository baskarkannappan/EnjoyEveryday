using System.Security.Claims;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EnjoyEveryday.Application.Services;

public class AppIdentityContext : IUserContext, ITenantContext
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider? _serviceProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IHostEnvironment? _hostEnvironment;

    public AppIdentityContext(
        IConfiguration configuration, 
        IServiceProvider? serviceProvider = null,
        IHttpContextAccessor? httpContextAccessor = null,
        IHostEnvironment? hostEnvironment = null)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
        _hostEnvironment = hostEnvironment;
    }

    private bool IsDevelopment()
    {
        if (_hostEnvironment != null)
        {
            return _hostEnvironment.IsDevelopment();
        }

        var envVar = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        if (!string.IsNullOrEmpty(envVar))
        {
            return string.Equals(envVar, "Development", StringComparison.OrdinalIgnoreCase);
        }

        var configEnv = _configuration["ASPNETCORE_ENVIRONMENT"]
                     ?? _configuration["DOTNET_ENVIRONMENT"]
                     ?? _configuration["Environment"];
        if (!string.IsNullOrEmpty(configEnv))
        {
            return string.Equals(configEnv, "Development", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private ClaimsPrincipal? User => ResolveUser();

    private ClaimsPrincipal? ResolveUser()
    {
        var httpUser = _httpContextAccessor?.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
            return httpUser;

        if (_serviceProvider != null)
        {
            try
            {
                var authProviderType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider"))
                    .FirstOrDefault(t => t != null);

                if (authProviderType != null)
                {
                    var authProvider = _serviceProvider.GetService(authProviderType);
                    if (authProvider != null)
                    {
                        var method = authProviderType.GetMethod("GetAuthenticationStateAsync");
                        if (method != null)
                        {
                            var task = method.Invoke(authProvider, null) as System.Threading.Tasks.Task;
                            if (task != null)
                            {
                                task.GetAwaiter().GetResult();
                                var resultProp = task.GetType().GetProperty("Result");
                                var authState = resultProp?.GetValue(task);
                                if (authState != null)
                                {
                                    var userProp = authState.GetType().GetProperty("User");
                                    var user = userProp?.GetValue(authState) as ClaimsPrincipal;
                                    if (user?.Identity?.IsAuthenticated == true)
                                        return user;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback gracefully
            }
        }

        return httpUser;
    }

    private bool HasValidAuthenticatedIdentity
    {
        get
        {
            var user = User;
            return user?.Identity?.IsAuthenticated == true;
        }
    }

    private bool IsAdminUser =>
        Roles.Contains("PlatformAdmin", StringComparer.OrdinalIgnoreCase) ||
        Roles.Contains("OrganizationOwner", StringComparer.OrdinalIgnoreCase);

    // ITenantContext
    public Guid TenantId
    {
        get
        {
            var user = User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var tenantClaim = user.FindFirst("TenantId")?.Value;
                if (Guid.TryParse(tenantClaim, out var tenantId))
                    return tenantId;

                return Guid.Empty;
            }
            
            // Fallback strictly in verified Development environment for unauthenticated sessions
            if (IsDevelopment())
            {
                var devTenant = _configuration["DevTenantId"];
                if (Guid.TryParse(devTenant, out var devTenantId))
                    return devTenantId;

                return Guid.Parse("10000000-0000-0000-0000-000000000001");
            }
                
            return Guid.Empty;
        }
    }

    public string TenantName => "Resolved Tenant";
    public bool IsResolved => TenantId != Guid.Empty;

    // IUserContext
    public Guid UserId 
    {
        get 
        {
            var user = User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(nameIdentifier, out var userId))
                    return userId;

                return Guid.Empty;
            }
            
            // Fallback strictly in verified Development environment
            if (IsDevelopment())
            {
                var devUser = _configuration["DevUserId"];
                if (Guid.TryParse(devUser, out var devUserId))
                    return devUserId;

                return Guid.Parse("50000000-0000-0000-0000-000000000002");
            }

            return Guid.Empty;
        }
    }

    public string Email
    {
        get
        {
            var user = User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var email = user.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(email)) return email;
                return string.Empty;
            }
            return IsDevelopment() ? (_configuration["DevUserEmail"] ?? string.Empty) : string.Empty;
        }
    }
    
    public bool IsAuthenticated => HasValidAuthenticatedIdentity || (IsDevelopment() && UserId != Guid.Empty);

    public IEnumerable<string> Roles
    {
        get
        {
            var user = User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                if (roles.Count > 0) return roles;
            }

            if (IsDevelopment() && UserId != Guid.Empty)
            {
                var devRoles = _configuration.GetSection("DevRoles").Get<string[]>();
                if (devRoles != null && devRoles.Length > 0) return devRoles;

                return new[] { "PlatformAdmin", "OrganizationOwner", "OrganizationAdmin", "BranchAdmin" };
            }

            return Array.Empty<string>();
        }
    }
    
    public IEnumerable<string> Permissions
    {
        get
        {
            if (!IsAuthenticated)
                return Array.Empty<string>();

            var user = User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var perms = user.FindAll("Permission").Select(c => c.Value).ToList();
                if (perms.Count > 0) return perms;
            }

            if (IsDevelopment() && UserId != Guid.Empty)
            {
                return EnjoyEveryday.Shared.Authorization.Permissions.All;
            }

            return Array.Empty<string>();
        }
    }

    public Guid? OrganizationId
    {
        get
        {
            if (HasValidAuthenticatedIdentity)
            {
                var orgClaim = User!.FindFirst("ActiveOrganizationId")?.Value ?? User!.FindFirst("OrganizationId")?.Value;
                if (Guid.TryParse(orgClaim, out var orgId))
                    return orgId;

                return null;
            }
            
            if (IsDevelopment())
            {
                var devOrg = _configuration["DevOrganizationId"];
                if (Guid.TryParse(devOrg, out var devOrgId))
                    return devOrgId;

                return Guid.Parse("20000000-0000-0000-0000-000000000001");
            }

            return null;
        }
    }

    public Guid? BranchId
    {
        get
        {
            if (HasValidAuthenticatedIdentity)
            {
                var branchClaim = User!.FindFirst("ActiveBranchId")?.Value ?? User!.FindFirst("BranchId")?.Value;
                if (Guid.TryParse(branchClaim, out var branchId))
                    return branchId;

                return null;
            }
            return null;
        }
    }

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        if (!IsAuthenticated)
            return false;

        // PlatformAdmin or OrganizationOwner has platform/org administrative permissions
        if (IsAdminUser)
        {
            return true;
        }

        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}


