using EnjoyEveryday.Application;
using EnjoyEveryday.Infrastructure;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Daycare.Web.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using EnjoyEveryday.Daycare.Web.Endpoints;
using EnjoyEveryday.UI.Shared;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Enforce INR currency and formatting application-wide
var inrCulture = new System.Globalization.CultureInfo("en-IN");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = inrCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = inrCulture;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = builder.Environment.IsDevelopment();
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10);
        options.DisconnectedCircuitMaxRetained = 250;
        options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(30);
    })
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(45);
    });
builder.Services.AddFluentUIComponents();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

// Enable Brotli & Gzip Response Compression for fast asset transfers
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "image/svg+xml",
        "application/javascript",
        "text/css"
    });
});

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => 
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/access-denied";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
            ? CookieSecurePolicy.SameAsRequest 
            : CookieSecurePolicy.Always;
    })
    .AddGoogle(options =>
    {
        var clientId = builder.Configuration["Authentication:Google:ClientId"] 
                    ?? builder.Configuration["Google:ClientId"]
                    ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"] 
                        ?? builder.Configuration["Google:ClientSecret"]
                        ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

        options.ClientId = !string.IsNullOrWhiteSpace(clientId) ? clientId : "mock-client-id";
        options.ClientSecret = !string.IsNullOrWhiteSpace(clientSecret) ? clientSecret : "mock-client-secret";
        options.SignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        options.SaveTokens = true;
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var uri = context.RedirectUri;
            if (!uri.Contains("prompt="))
            {
                uri += (uri.Contains("?") ? "&" : "?") + "prompt=select_account";
            }
            context.Response.Redirect(uri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// Real-time SignalR and Notifications
builder.Services.AddSignalR();
builder.Services.AddSingleton<EnjoyEveryday.Daycare.Web.Services.DaycareRealtimeNotificationCenter>();
builder.Services.AddSingleton<EnjoyEveryday.Application.Services.IBranchStatusRealtimeNotifier, EnjoyEveryday.Daycare.Web.Services.BranchStatusRealtimeNotifier>();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "https://localhost:7005", "http://localhost:5076" };

    options.AddPolicy("AllowParentPortal", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Application and Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddEnjoyEverydayUiShared();
builder.Services.AddScoped<EnjoyEveryday.Daycare.Web.Services.StorybookStorageService>();
builder.Services.AddScoped<EnjoyEveryday.Shared.Authorization.IOrganizationPermissionService, EnjoyEveryday.Daycare.Web.Services.OrganizationPermissionService>();
builder.Services.AddScoped<EnjoyEveryday.Daycare.Web.Services.IPageAuthorizationService, EnjoyEveryday.Daycare.Web.Services.PageAuthorizationService>();
builder.Services.AddScoped<EnjoyEveryday.Daycare.Web.Services.IUserViewPreferenceService, EnjoyEveryday.Daycare.Web.Services.UserViewPreferenceService>();
builder.Services.AddScoped<EnjoyEveryday.Daycare.Web.Services.IBranchOperationalUiGuard, EnjoyEveryday.Daycare.Web.Services.BranchOperationalUiGuard>();
builder.Services.AddHostedService<EnjoyEveryday.Daycare.Web.Services.DaycareEndOfDayBackgroundService>();
builder.Services.AddHostedService<EnjoyEveryday.Daycare.Web.Services.MonthlyTuitionBillingBackgroundService>();

var app = builder.Build();

// Run database migrations on startup (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var migrationRunner = scope.ServiceProvider.GetRequiredService<SqlMigrationRunner>();
    var migrationsPath = Path.Combine(app.Environment.ContentRootPath, "..", "..", "database", "migrations");
    try
    {
        await migrationRunner.RunMigrationsAsync(migrationsPath);
        var imageMigration = scope.ServiceProvider.GetRequiredService<EnjoyEveryday.Infrastructure.Storage.Base64ImageMigrationService>();
        await imageMigration.MigrateExistingBase64ImagesAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database migration failed — app will continue. Ensure PostgreSQL is running.");
    }
}

// Configure the HTTP request pipeline.
app.UseAppObservability();
app.UseResponseCompression();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    var path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/_content/") || path.StartsWith("/css/") || path.StartsWith("/js/") || path.EndsWith(".css") || path.EndsWith(".js") || path.EndsWith(".woff2"))
    {
        context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
    }
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseCors("AllowParentPortal");

app.UseAuthentication();
app.UseAuthorization();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(inrCulture),
    SupportedCultures = new[] { inrCulture },
    SupportedUICultures = new[] { inrCulture }
});

app.UseAntiforgery();

app.MapStaticAssets();

app.MapAppHealthChecks();
app.MapAuthEndpoints();
app.MapFloorMapEndpoints();
app.MapMediaEndpoints();
app.MapStorybookEndpoints();
app.MapBillingEndpoints();
app.MapHub<EnjoyEveryday.Daycare.Web.Hubs.ClassroomChatHub>("/hubs/classroom-chat").RequireCors("AllowParentPortal");
app.MapRealtimeChatEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
