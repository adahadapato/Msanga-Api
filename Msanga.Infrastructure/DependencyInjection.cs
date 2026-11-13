using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Msanga.Application;
using Msanga.Infrastructure.Settings.Application;
using Msanga.Infrastructure.Settings.Email;
using Msanga.Infrastructure.Settings.Identity;
using Msanga.Persistence;

namespace Msanga.Infrastructure;

/// <summary>
/// Registers Application, Persistence and everything Infrastructure provides: settings, email and accounts.
/// </summary>
/// <remarks>Called once, from Presentation's DependencyInjection.</remarks>
public static class DependencyInjection
{
    /// <summary>Adds the Infrastructure layer (and the layers it composes).</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();                   // MediatR pipeline, validators, TimeProvider
        services.AddPersistence(configuration);      // SQL Server DbContext, unit of work, repositories

        services.AddApplicationSettings(configuration);
        services.AddEmail(configuration);
        services.AddMsangaIdentity(configuration);

        return services;
    }
}
