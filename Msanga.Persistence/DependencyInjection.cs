using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Msanga.Domain.Constants;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Interfaces.Identity;
using Msanga.Persistence.Interceptors;
using Msanga.Persistence.Repositories;
using Msanga.Persistence.Repositories.Identity;

namespace Msanga.Persistence;

/// <summary>
/// Registers everything Persistence provides, in this order:
/// <list type="number">
///   <item><description><b>Connection string:</b> read and checked (fails fast if missing).</description></item>
///   <item><description><b>Interceptors:</b> the audit interceptor (CreatedAt / UpdatedAt).</description></item>
///   <item><description><b>Database context:</b> SQL Server, migrations in this project.</description></item>
///   <item><description><b>Unit of work:</b> the same context instance, seen as <see cref="IUnitOfWork"/>.</description></item>
///   <item><description><b>Repositories:</b> members, and the sign-in tables.</description></item>
/// </list>
/// </summary>
/// <remarks>Called once, from Infrastructure's DependencyInjection.</remarks>
public static class DependencyInjection
{
    /// <summary>Adds the database and its services.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration (the connection string comes from User Secrets /
    /// environment variables, never appsettings.json).</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="InvalidOperationException">No connection string configured.</exception>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetConnectionString(configuration);   // 1

        services.AddSingleton<AuditInterceptor>();                   // 2 (no state: only the clock)

        services.AddDbContext<MsangaDbContext>((provider, options) => // 3
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(MsangaDbContext).Assembly.FullName);
                // No EnableRetryOnFailure: registration runs in a transaction (UserManager + our tables), and the
                // retrying strategy refuses transactions started by hand.
            });
            options.AddInterceptors(provider.GetRequiredService<AuditInterceptor>());

            // Expected by design: Members has a soft-delete filter, AspNetUsers doesn't (see EntityConfiguration)
            options.ConfigureWarnings(warnings => warnings.Ignore(
                CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<MsangaDbContext>());   // 4

        services.AddScoped<IMemberRepository, MemberRepository>();                                     // 5
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAccountCodeRepository, AccountCodeRepository>();

        return services;
    }

    /// <summary>Reads the connection string and stops start-up if it's missing.</summary>
    private static string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(AppSettings.ConnectionStrings.DefaultName);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"'{AppSettings.ConnectionStrings.Default}' is empty. Set it in User Secrets (development) or as the " +
                "environment variable ConnectionStrings__DefaultConnection (production). See README.md.");

        return connectionString;
    }
}
