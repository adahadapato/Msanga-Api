using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Msanga.Infrastructure.Settings.Identity;

namespace Msanga.Presentation.Settings.RateLimiting;

/// <summary>
/// Limits on the endpoints robots like most: the public account endpoints, per connection (IP address), on top of
/// Identity's own lockout after wrong passwords; and adding schools, per member. In memory: a restart resets them.
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

    /// <summary>Adding a school: 20 tries a day per member (a "Did you mean…?" and its "add anyway" are two), so
    /// nobody can flood the list.</summary>
    public const string AddCommunityPolicy = "add-community";

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
            options.AddPolicy(AddCommunityPolicy, context => FixedWindowPerMember(context, 20, TimeSpan.FromDays(1)));

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

    /// <summary>Per signed-in member (the access token's user Id); per IP address if there isn't one.</summary>
    private static RateLimitPartition<string> FixedWindowPerMember(HttpContext context, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirstValue(JwtClaimNames.UserId) is { } userId
                ? $"member:{userId}"
                : context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 });

    private static RateLimitPartition<string> FixedWindow(HttpContext context, int permits, TimeSpan window) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = window, QueueLimit = 0 });
}
