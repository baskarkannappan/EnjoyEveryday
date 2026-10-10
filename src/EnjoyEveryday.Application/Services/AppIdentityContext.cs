using System.Security.Claims;
using EnjoyEveryday.Shared.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace EnjoyEveryday.Application.Services;

public class AppIdentityContext : IUserContext, ITenantContext
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly bool _isDevelopment;

    public AppIdentityContext(IConfiguration configuration, IHttpContextAccessor? httpContextAccessor = null)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    }

    private ClaimsPrincipal? User => _httpContextAccessor?.HttpContext?.User;

    private bool HasValidAuthenticatedIdentity => User != null && User.Identity?.IsAuthenticated == true;

    // ITenantContext
    public Guid TenantId
    {
        get
        {
            if (HasValidAuthenticatedIdentity)
            {
                // In Stage 2/3, this claim will only be issued AFTER server-side database verification 
                // of the user's active membership in this tenant.
                var tenantClaim = User!.FindFirst("TenantId")?.Value;
                if (Guid.TryParse(tenantClaim, out var tenantId))
                    return tenantId;
            }
            
            // Fallback ONLY in Development. Never allowed in Production.
            if (_isDevelopment)
            {
                var devTenant = _configuration["DevTenantId"];
                if (Guid.TryParse(devTenant, out var devTenantId))
                    return devTenantId;
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
            
            if (_isDevelopment)
            {
                var devUser = _configuration["DevUserId"];
                if (Guid.TryParse(devUser, out var devUserId))
                    return devUserId;
            }

            return Guid.Empty;
        }
    }

    public string Email => HasValidAuthenticatedIdentity ? (User!.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty) : string.Empty;
    
    // Fallback users are NEVER considered authenticated
    public bool IsAuthenticated => HasValidAuthenticatedIdentity;

    public IEnumerable<string> Roles => HasValidAuthenticatedIdentity 
        ? User!.FindAll(ClaimTypes.Role).Select(c => c.Value) 
        : Array.Empty<string>();
    
    public IEnumerable<string> Permissions => HasValidAuthenticatedIdentity
        ? User!.FindAll("Permission").Select(c => c.Value) 
        : Array.Empty<string>();

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
            return null;
        }
    }

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
}
