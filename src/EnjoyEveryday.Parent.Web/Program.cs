using EnjoyEveryday.Application;
using EnjoyEveryday.Infrastructure;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Parent.Web.Components;
using Microsoft.FluentUI.AspNetCore.Components;

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
builder.Services.AddScoped<EnjoyEveryday.Parent.Web.Services.ParentSessionState>();

builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => 
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.AccessDeniedPath = "/auth/access-denied";
        options.Cookie.HttpOnly = true;
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

// Application and Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

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
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

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
EnjoyEveryday.Parent.Web.Endpoints.AuthEndpoints.MapAuthEndpoints(app);
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
