using EnjoyEveryday.Application;
using EnjoyEveryday.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<EnjoyEveryday.Api.Middleware.GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "https://localhost:7003", "https://localhost:7004", "https://localhost:7005" };

    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EnjoyEveryday.Api.Mcp.McpToolExecutor>();

// Microsoft Native Output Caching with Multi-Tenant isolation
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("TenantReadCache", policy =>
        policy.Expire(TimeSpan.FromSeconds(30))
              .SetVaryByHeader("X-Tenant-Id")
              .SetVaryByQuery("*")
              .Tag("daycare-reads"));

    options.AddPolicy("DashboardMetricsCache", policy =>
        policy.Expire(TimeSpan.FromSeconds(15))
              .SetVaryByHeader("X-Tenant-Id")
              .Tag("dashboard-metrics"));
});

// Microsoft Native Rate Limiting (sliding window per tenant/client)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "API rate limit reached. Please pause briefly before submitting additional requests.",
            Instance = context.HttpContext.Request.Path
        }, cancellationToken);
    };

    options.AddPolicy("ApiSlidingWindow", httpContext =>
    {
        var tenantId = httpContext.Request.Headers["X-Tenant-Id"].ToString();
        var partitionKey = !string.IsNullOrWhiteSpace(tenantId)
            ? $"tenant_{tenantId}"
            : (httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous");

        return System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 20,
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
            });
    });
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "SmartAuth";
        options.DefaultChallengeScheme = "SmartAuth";
    })
    .AddPolicyScheme("SmartAuth", "Cookie or Agent Bearer", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return EnjoyEveryday.Api.Authentication.AgentAuthenticationDefaults.AuthenticationScheme;
            }
            return CookieAuthenticationDefaults.AuthenticationScheme;
        };
    })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, EnjoyEveryday.Api.Authentication.AgentTokenAuthenticationHandler>(
        EnjoyEveryday.Api.Authentication.AgentAuthenticationDefaults.AuthenticationScheme, _ => { })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization();

// Application and Infrastructure layer registrations
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAppObservability();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("DefaultCors");

app.UseRateLimiter();
app.UseOutputCache();

app.UseAuthentication();
app.UseAuthorization();

app.MapAppHealthChecks();
app.MapControllers().RequireRateLimiting("ApiSlidingWindow");

app.Run();
