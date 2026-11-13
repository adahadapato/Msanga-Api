using Msanga.Presentation.Endpoints.Auth;
using Msanga.Presentation.Endpoints.Hello;
using Msanga.Presentation.Endpoints.Members;

namespace Msanga.Presentation.Extensions;

/// <summary>Every route of the API, in one place.</summary>
public static class EndpointExtensions
{
    /// <summary>Maps all endpoint groups under <c>/api</c>, plus <c>/health</c>.</summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication MapMsangaEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapHelloEndpoints();             // /api/hello
        api.MapAuthEndpoints();              // /api/auth/…
        api.MapMemberEndpoints();            // /api/me

        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
           .ExcludeFromDescription();

        return app;
    }
}
