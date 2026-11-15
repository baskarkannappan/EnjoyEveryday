using Microsoft.AspNetCore.Routing;
using EnjoyEveryday.Infrastructure.Endpoints;

namespace EnjoyEveryday.Daycare.Web.Endpoints;

public static class MediaEndpoints
{
    // Preserves existing Daycare.Web using EnjoyEveryday.Daycare.Web.Endpoints namespace
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return EnjoyEveryday.Infrastructure.Endpoints.MediaEndpoints.MapMediaEndpoints(endpoints);
    }
}
