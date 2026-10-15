using System.Security.Claims;
using EnjoyEveryday.Shared.Authorization;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace EnjoyEveryday.Application.Services;

public class AppIdentityContext : IUserContext, ITenantContext
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider? _serviceProvider;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppIdentityContext(
        IConfiguration configuration, 
        IServiceProvider? serviceProvider = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    private bool IsDevelopment()
    {
        var envVar = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        if (!string.IsNullOrEmpty(envVar))
            return string.Equals(envVar, "Development", StringComparison.OrdinalIgnoreCase);

        var configEnv = _configuration["ASPNETCORE_ENVIRONMENT"]
                     ?? _configuration["DOTNET_ENVIRONMENT"]
                     ?? _configuration["Environment"];
        if (!string.IsNullOrEmpty(configEnv))
            return string.Equals(configEnv, "Development", StringComparison.OrdinalIgnoreCase);

        // In local development, if DevUserId or DevOrganizationId is configured in appsettings.json, treat as Development
        return !string.IsNullOrEmpty(_configuration["DevUserId"]) || !string.IsNullOrEmpty(_configuration["DevOrganizationId"]);
    }

    private ClaimsPrincipal? User => ResolveUser();

    private ClaimsPrincipal? ResolveUser()
    {
        if (_httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            return _httpContextAccessor.HttpContext.User;

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

        return _httpContextAccessor?.HttpContext?.User;
    }

    private bool HasValidAuthenticatedIdentity => User != null && User.Identity?.IsAuthenticated == true;

    private bool IsAdminUser =>
        Email.Equals("admin@littlestars.com", StringComparison.OrdinalIgnoreCase) ||
        UserId == Guid.Parse("50000000-0000-0000-0000-000000000002") ||
        Roles.Contains("PlatformAdmin", StringComparer.OrdinalIgnoreCase) ||
        Roles.Contains("OrganizationOwner", StringComparer.OrdinalIgnoreCase);

    // ITenantContext
    public Guid TenantId
    {
        get
        {
            if (HasValidAuthenticatedIdentity)
            {
                var tenantClaim = User!.FindFirst("TenantId")?.Value;
                if (Guid.TryParse(tenantClaim, out var tenantId))
                    return tenantId;
            }
            
            // Fallback in Development
            if (IsDevelopment() || IsAdminUser)
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
            if (HasValidAuthenticatedIdentity)
            {
                var nameIdentifier = User!.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(nameIdentifier, out var userId))
                    return userId;
            }
            
            if (IsDevelopment())
            {
                var devUser = _configuration["DevUserId"];
                if (Guid.TryParse(devUser, out var devUserId))
                    return devUserId;

                // Default to admin@littlestars.com
                return Guid.Parse("50000000-0000-0000-0000-000000000002");
            }

            return Guid.Empty;
        }
    }

    public string Email
    {
        get
        {
            if (HasValidAuthenticatedIdentity)
            {
                var email = User!.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrEmpty(email)) return email;
            }
            return IsDevelopment() ? (_configuration["DevUserEmail"] ?? "admin@littlestars.com") : string.Empty;
        }
    }
    
    public bool IsAuthenticated => HasValidAuthenticatedIdentity;

    public IEnumerable<string> Roles
    {
        get
        {
            if (HasValidAuthenticatedIdentity)
            {
                var roles = User!.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                if (roles.Count > 0) return roles;
                if (IsDevelopment() || Email.Equals("admin@littlestars.com", StringComparison.OrdinalIgnoreCase))
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

            if (IsAdminUser)
            {
                return EnjoyEveryday.Shared.Authorization.Permissions.All;
            }

            if (HasValidAuthenticatedIdentity)
            {
                var perms = User!.FindAll("Permission").Select(c => c.Value).ToList();
                if (perms.Count > 0) return perms;
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
                var orgClaim = User!.FindFirst("ActiveOrganizationId")?.Value;
                if (Guid.TryParse(orgClaim, out var orgId))
                    return orgId;
            }
            
            var devOrg = _configuration["DevOrganizationId"];
            if (Guid.TryParse(devOrg, out var devOrgId))
                return devOrgId;

            // Default to Little Stars Daycare organization
            return Guid.Parse("20000000-0000-0000-0000-000000000001");
        }
    }

    public bool HasPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        if (!IsAuthenticated)
            return false;

        // admin@littlestars.com, PlatformAdmin, or OrganizationOwner has full permissions across the application
        if (IsAdminUser)
        {
            return true;
        }

        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}

