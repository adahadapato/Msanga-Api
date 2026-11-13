using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Msanga.Application.Common.Behaviours;

namespace Msanga.Application;

/// <summary>
/// Registers the Application layer: the clock, the validators and the MediatR pipeline.
/// </summary>
/// <remarks>Uses only DI abstractions, so Application stays free of ASP.NET Core. Called by Infrastructure.</remarks>
public static class DependencyInjection
{
    /// <summary>Adds the Application layer's services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // 1. The clock: .NET's built-in TimeProvider (FakeTimeProvider in tests)
        services.AddSingleton(TimeProvider.System);

        // 2. Validators: without this, ValidationBehavior finds none and NO validation runs
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // 3. MediatR: outermost first: Logging → Validation → handler
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            // MediatR 13+ asks for a licence key (free for small organisations): cfg.LicenseKey = "...";
        });

        return services;
    }
}
