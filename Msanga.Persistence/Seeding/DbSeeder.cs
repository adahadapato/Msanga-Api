using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Msanga.Persistence.Configurations.Identity;

namespace Msanga.Persistence.Seeding;

/// <summary>
/// At start-up: applies pending database migrations, then creates the roles.
/// </summary>
/// <remarks>
/// <para>Any failure is logged and stops start-up: a half-ready database is worse than an API that won't start.</para>
/// <para>If the project has no migrations yet, start-up stops with the command that creates the first one.</para>
/// </remarks>
public static class DbSeeder
{
    /// <summary>Migrates and seeds the database.</summary>
    /// <param name="services">The app's root service provider.</param>
    /// <param name="cancellationToken">Stops start-up.</param>
    /// <exception cref="InvalidOperationException">No migrations exist yet.</exception>
    public static async Task SeedDatabaseAsync(this IServiceProvider services,
                                               CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));
        var context = provider.GetRequiredService<MsangaDbContext>();

        if (!context.Database.GetMigrations().Any())
        {
            const string message =
                "The database has no migrations yet. Create the first one (once), then start the API again: " +
                "dotnet ef migrations add InitialAccounts --project Msanga.Persistence --startup-project Msanga.Presentation";
            logger.LogCritical(message);
            throw new InvalidOperationException(message);
        }

        await RunStepAsync(logger, "1: migrations", () => context.Database.MigrateAsync(cancellationToken));

        await RunStepAsync(logger, "2: roles",
            () => ApplicationRoleSeeder.SeedAsync(provider.GetRequiredService<RoleManager<ApplicationRole>>(),
                                                  logger, cancellationToken));

        logger.LogInformation("Database initialization completed.");
    }

    /// <summary>Runs one step, logging its start, end or failure.</summary>
    private static async Task RunStepAsync(ILogger logger, string step, Func<Task> run)
    {
        logger.LogInformation("Database initialization step {Step}: starting.", step);
        try
        {
            await run();
            logger.LogInformation("Database initialization step {Step}: done.", step);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database initialization step {Step} FAILED. Start-up cannot continue.", step);
            throw;
        }
    }
}
