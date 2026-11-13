using System.Text.Json.Serialization;
using Msanga.Infrastructure;
using Msanga.Presentation.Middleware;
using Msanga.Presentation.Settings.Authentication;
using Msanga.Presentation.Settings.Cors;
using Msanga.Presentation.Settings.OpenApi;
using Msanga.Presentation.Settings.RateLimiting;

namespace Msanga.Presentation;

/// <summary>
/// The composition root: every layer, then what's specific to hosting the API (OpenAPI, sign-in, CORS, rate
/// limits, errors, JSON).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Adds everything the API needs.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Layers: Infrastructure composes Application and Persistence (once each)
        services.AddInfrastructure(configuration);

        // 2. API
        services.AddMsangaOpenApi(configuration);         // 2a. OpenAPI document + Swagger page
        services.AddMsangaAuthentication();               // 2b. JWT bearer, the policies, CurrentUser
        services.AddMsangaCors(configuration);            //     the web app may call the API from a browser
        services.AddMsangaRateLimiting();                 //     limits on the public account endpoints

        // 2c. Errors: one ProblemDetails shape for thrown exceptions AND Result failures
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // 2d. JSON: enums as names, both in and out; numbers are still accepted
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return services;
    }
}
