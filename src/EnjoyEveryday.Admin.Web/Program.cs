using EnjoyEveryday.Application;
using EnjoyEveryday.Infrastructure;
using EnjoyEveryday.Infrastructure.Data;
using EnjoyEveryday.Shared.Data;
using EnjoyEveryday.Admin.Web.Components;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true);
builder.Services.AddFluentUIComponents();
builder.Services.AddHttpClient();

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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
