namespace Msanga.Presentation.Endpoints.Hello;

/// <summary>
/// <c>GET /api/hello</c>: M0's greeting, so the web app's connection check (Settings) can prove it reaches the API.
/// </summary>
public static class HelloEndpoints
{
    /// <summary>The greeting, as the web app's <c>Greeting</c> type (src/api/hello.ts).</summary>
    /// <param name="Title">"Mme nsanga!" ("my friends" in Annang).</param>
    /// <param name="Message">"Your set is waiting."</param>
    /// <param name="ServerTime">When the API answered (UTC).</param>
    public sealed record Greeting(string Title, string Message, DateTime ServerTime);

    /// <summary>Maps the route.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapHelloEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/hello", (TimeProvider clock) =>
                Results.Ok(new Greeting("Mme nsanga!", "Your set is waiting.", clock.GetUtcNow().UtcDateTime)))
            .AllowAnonymous()
            .WithTags("Hello")
            .WithName("Hello")
            .WithSummary("A greeting: proves the API is running and reachable")
            .Produces<Greeting>();

        return api;
    }
}
