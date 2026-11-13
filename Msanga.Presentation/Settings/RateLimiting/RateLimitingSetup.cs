using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Msanga.Presentation.Settings.RateLimiting;

/// <summary>
/// Limits on the public account endpoints robots like most. Per connection (IP address), in memory: a restart
/// resets them. On top of Identity's own lockout after wrong passwords.
/// </summary>
public static class RateLimitingSetup
{
    /// <summary>Sign in and refresh.</summary>
    public const string SignInPolicy = "sign-in";

    /// <summary>Endpoints that SEND an email (create account, resend confirmation, forgot password): kept low so
    /// nobody can use Msanga to flood an inbox.</summary>
    public const string AccountEmailPolicy = "account-email";

    /// <summary>Typing an emailed code (confirm email, reset password): on top of the 5 tries each code allows.</summary>
    public const string AccountCodePolicy = "account-code";

    /// <summary>Adds the rate limiter and its policies.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddMsangaRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(SignInPolicy, context => FixedWindow(context, 20, TimeSpan.FromMinutes(5)));
            options.AddPolicy(AccountEmailPolicy, context => FixedWindow(context, 5, TimeSpan.FromMinutes(15)));
            options.AddPolicy(AccountCodePolicy, context => FixedWindow(context, 10, TimeSpan.FromMinutes(5)));

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc6585#section-4",
                    title = "Too many requests",
                    status = StatusCodes.Status429TooManyRequests,
                    code = "RateLimit.TooManyRequests",
                    detail = "That's a lot of tries in a short time. Please wait a few minutes and try again."
                }, ct);
            };
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindow(HttpContext context, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 });
}
