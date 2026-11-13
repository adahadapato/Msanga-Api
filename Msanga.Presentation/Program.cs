using Msanga.Persistence.Seeding;
using Msanga.Presentation;
using Msanga.Presentation.Extensions;
using Msanga.Presentation.Settings.Authentication;
using Msanga.Presentation.Settings.Cors;
using Msanga.Presentation.Settings.OpenApi;
using Msanga.Worker;
using Serilog;

// Bootstrap logger: for failures before the app's own logging (from appsettings.json) is running
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog, from the "Serilog" section of appsettings.json
    builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

    // Every layer (one composition root)
    builder.Services
        .AddPresentation(builder.Configuration)
        .AddWorker(builder.Configuration);

    var app = builder.Build();

    // Migrations and roles (stops with a clear message if the first migration hasn't been created yet)
    await app.Services.SeedDatabaseAsync();

    // HTTP pipeline
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }
    app.UseMsangaOpenApi();          // /swagger (when OpenApi:Enabled)
    app.UseMsangaCors();             // before authentication: browser preflights need no sign-in
    app.UseMsangaAuthentication();
    app.UseRateLimiter();            // after authentication; per-endpoint policies (account endpoints)

    app.MapMsangaEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is how "dotnet ef" stops the app after reading its services: not a failure
    Log.Fatal(ex, "Msanga API failed to start.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
