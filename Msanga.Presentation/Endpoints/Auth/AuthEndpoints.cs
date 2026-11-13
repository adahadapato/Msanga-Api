using MediatR;
using Msanga.Application.Features.Auth.Commands;
using Msanga.Application.Features.Auth.Dtos;
using Msanga.Domain.Constants;
using Msanga.Presentation.Extensions;
using Msanga.Presentation.Settings.RateLimiting;

namespace Msanga.Presentation.Endpoints.Auth;

/// <summary>
/// <c>/api/auth</c>: create account, confirm email, sign in, tokens, forgot / reset / change password. The web app's
/// pages of the same names call these (see msanga-web src/api/authTypes.ts).
/// </summary>
/// <remarks>
/// Privacy: register, resend-confirmation and forgot-password answer 204 whether or not the email has an account.
/// </remarks>
public static class AuthEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Auth");

        // ───────────── Create account ─────────────

        auth.MapPost("/register", async (RegisterCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSetup.AccountEmailPolicy)
            .WithName("Register")
            .WithSummary("Create an account; a 6-digit confirmation code is emailed")
            .WithDescription("Answers 204 even when the email already has an account (its owner gets an email " +
                             "instead), so nobody can use this to find out who is on Msanga.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/confirm-email", async (ConfirmEmailCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSetup.AccountCodePolicy)
            .WithName("ConfirmEmail")
            .WithSummary("Confirm the email: { email, code } with the 6-digit code from the email")
            .WithDescription("A wrong, expired or used code, an unknown email and an already-confirmed email all get " +
                             "the same 400 Auth.InvalidCode. Each code allows 5 tries.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        auth.MapPost("/resend-confirmation", async (ResendConfirmationCommand command, ISender sender,
                                                     CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSetup.AccountEmailPolicy)
            .WithName("ResendConfirmation")
            .WithSummary("Email a new confirmation code (always 204)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        // ───────────── Sign in / out ─────────────

        auth.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSetup.SignInPolicy)
            .WithName("Login")
            .WithSummary("Sign in with email and password; returns an access + refresh token and the member")
            .WithDescription("A wrong email and a wrong password get the same 401. The email must be confirmed " +
                             "(403 Auth.EmailNotConfirmed). Five wrong passwords lock the account for a while.")
            .Produces<AuthTokensDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        auth.MapPost("/refresh", async (RefreshTokenCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSetup.SignInPolicy)
            .WithName("RefreshToken")
            .WithSummary("Swap the refresh token for a new pair (the old refresh token stops working)")
            .Produces<AuthTokensDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/logout", async (LogoutCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .WithName("Logout")
            .WithSummary("Sign out: the refresh token can't be used again")
            .Produces(StatusCodes.Status204NoContent);

        // ───────────── Passwords ─────────────

        auth.MapPost("/forgot-password", async (ForgotPasswordCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSetup.AccountEmailPolicy)
            .WithName("ForgotPassword")
            .WithSummary("Email a 6-digit reset-password code (always 204)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        auth.MapPost("/reset-password", async (ResetPasswordCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingSetup.AccountCodePolicy)
            .WithName("ResetPassword")
            .WithSummary("Choose a new password: { email, code, newPassword }; signs out every session")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        auth.MapPut("/password", async (ChangePasswordCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .RequireAuthorization(Policies.RequireMember)
            .WithName("ChangePassword")
            .WithSummary("Change my password; signs out every session, so sign in again afterwards")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();

        return api;
    }
}
