using EnjoyEveryday.Application;
using EnjoyEveryday.Infrastructure;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Daycare.Web.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using EnjoyEveryday.Daycare.Web.Endpoints;
using EnjoyEveryday.UI.Shared;

var builder = WebApplication.CreateBuilder(args);

// Enforce INR currency and formatting application-wide
var inrCulture = new System.Globalization.CultureInfo("en-IN");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = inrCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = inrCulture;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true)
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 10 * 1024 * 1024);
builder.Services.AddFluentUIComponents();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

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
    });

builder.Services.AddAuthorization();

// Real-time SignalR and Notifications
builder.Services.AddSignalR();
builder.Services.AddSingleton<EnjoyEveryday.Daycare.Web.Services.DaycareRealtimeNotificationCenter>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowParentPortal", policy =>
    {
        policy.WithOrigins("https://localhost:7005", "http://localhost:5076")
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
builder.Services.AddScoped<EnjoyEveryday.Daycare.Web.Services.IOrganizationPermissionService, EnjoyEveryday.Daycare.Web.Services.OrganizationPermissionService>();
builder.Services.AddScoped<EnjoyEveryday.Daycare.Web.Services.IPageAuthorizationService, EnjoyEveryday.Daycare.Web.Services.PageAuthorizationService>();

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
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database migration failed — app will continue. Ensure PostgreSQL is running.");
    }
}

// Configure the HTTP request pipeline.
app.UseAppObservability();

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
app.MapStorybookEndpoints();
app.MapBillingEndpoints();
app.MapHub<EnjoyEveryday.Daycare.Web.Hubs.ClassroomChatHub>("/hubs/classroom-chat").RequireCors("AllowParentPortal");
app.MapRealtimeChatEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
