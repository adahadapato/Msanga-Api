using MediatR;
using Msanga.Application.Features.Auth.Dtos;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Shared.Results;

namespace Msanga.Application.Features.Auth.Commands;

// ───────────────────────────── Create account ─────────────────────────────

/// <summary>Create account. A 6-digit confirmation code is emailed; the member types it in, then signs in.</summary>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="SchoolSurname">Surname at school, if different now (optional).</param>
/// <param name="Email">Email address.</param>
/// <param name="Password">Chosen password.</param>
public sealed record RegisterCommand(string FirstName, string LastName, string? SchoolSurname, string Email,
                                     string Password) : IRequest<Result>;

/// <summary>Handles <see cref="RegisterCommand"/>.</summary>
public sealed class RegisterCommandHandler(IAuthService auth) : IRequestHandler<RegisterCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(RegisterCommand c, CancellationToken ct) =>
        auth.RegisterAsync(new NewAccount(c.FirstName.Trim(), c.LastName.Trim(), c.SchoolSurname?.Trim(),
                                          c.Email.Trim(), c.Password), ct);
}

/// <summary>"Check your email": the email and the 6-digit code typed into the page.</summary>
/// <param name="Email">The account's email.</param>
/// <param name="Code">The code, e.g. "482913" or "482 913" (spaces and dashes are ignored).</param>
public sealed record ConfirmEmailCommand(string Email, string Code) : IRequest<Result>;

/// <summary>Handles <see cref="ConfirmEmailCommand"/>.</summary>
public sealed class ConfirmEmailCommandHandler(IAuthService auth) : IRequestHandler<ConfirmEmailCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ConfirmEmailCommand c, CancellationToken ct) =>
        auth.ConfirmEmailAsync(c.Email.Trim(), CodeText.Digits(c.Code), ct);
}

/// <summary>Turns what was typed into the bare digits ("482 913" → "482913").</summary>
public static class CodeText
{
    /// <summary>Removes spaces and dashes.</summary>
    /// <param name="typed">What the member typed.</param>
    /// <returns>The digits.</returns>
    public static string Digits(string? typed) =>
        new((typed ?? string.Empty).Where(c => c is not (' ' or '-')).ToArray());
}

/// <summary>"Send a new code". Always succeeds (it never says whether the email has an account).</summary>
/// <param name="Email">The address.</param>
public sealed record ResendConfirmationCommand(string Email) : IRequest<Result>;

/// <summary>Handles <see cref="ResendConfirmationCommand"/>.</summary>
public sealed class ResendConfirmationCommandHandler(IAuthService auth)
    : IRequestHandler<ResendConfirmationCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ResendConfirmationCommand c, CancellationToken ct) =>
        auth.ResendConfirmationAsync(c.Email.Trim(), ct);
}

// ───────────────────────────── Sign in / out ─────────────────────────────

/// <summary>Sign in with email and password.</summary>
/// <param name="Email">The account's email.</param>
/// <param name="Password">The password.</param>
public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthTokensDto>>;

/// <summary>Handles <see cref="LoginCommand"/>.</summary>
public sealed class LoginCommandHandler(IAuthService auth) : IRequestHandler<LoginCommand, Result<AuthTokensDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthTokensDto>> Handle(LoginCommand c, CancellationToken ct) =>
        (await auth.LoginAsync(c.Email.Trim(), c.Password, ct)).Map(AuthTokensDto.From);
}

/// <summary>Swaps a refresh token for a new pair (the app stays signed in).</summary>
/// <param name="RefreshToken">The refresh token.</param>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthTokensDto>>;

/// <summary>Handles <see cref="RefreshTokenCommand"/>.</summary>
public sealed class RefreshTokenCommandHandler(IAuthService auth)
    : IRequestHandler<RefreshTokenCommand, Result<AuthTokensDto>>
{
    /// <inheritdoc />
    public async Task<Result<AuthTokensDto>> Handle(RefreshTokenCommand c, CancellationToken ct) =>
        (await auth.RefreshTokenAsync(c.RefreshToken, ct)).Map(AuthTokensDto.From);
}

/// <summary>Sign out: the refresh token stops working.</summary>
/// <param name="RefreshToken">The refresh token.</param>
public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;

/// <summary>Handles <see cref="LogoutCommand"/>.</summary>
public sealed class LogoutCommandHandler(IAuthService auth) : IRequestHandler<LogoutCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(LogoutCommand c, CancellationToken ct) => auth.RevokeTokenAsync(c.RefreshToken, ct);
}

// ───────────────────────────── Passwords ─────────────────────────────

/// <summary>"Forgot your password?". Always succeeds (it never says whether the email has an account).</summary>
/// <param name="Email">The address.</param>
public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;

/// <summary>Handles <see cref="ForgotPasswordCommand"/>.</summary>
public sealed class ForgotPasswordCommandHandler(IAuthService auth) : IRequestHandler<ForgotPasswordCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ForgotPasswordCommand c, CancellationToken ct) =>
        auth.RequestPasswordResetAsync(c.Email.Trim(), ct);
}

/// <summary>"Choose a new password": the email, the 6-digit code from the email and the new password.</summary>
/// <param name="Email">The account's email.</param>
/// <param name="Code">The code (spaces and dashes are ignored).</param>
/// <param name="NewPassword">The new password.</param>
public sealed record ResetPasswordCommand(string Email, string Code, string NewPassword) : IRequest<Result>;

/// <summary>Handles <see cref="ResetPasswordCommand"/>.</summary>
public sealed class ResetPasswordCommandHandler(IAuthService auth) : IRequestHandler<ResetPasswordCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ResetPasswordCommand c, CancellationToken ct) =>
        auth.ResetPasswordAsync(c.Email.Trim(), CodeText.Digits(c.Code), c.NewPassword, ct);
}

/// <summary>Settings → change password (signs out every session, so sign in again afterwards).</summary>
/// <param name="CurrentPassword">The password now.</param>
/// <param name="NewPassword">The new password.</param>
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;

/// <summary>Handles <see cref="ChangePasswordCommand"/>.</summary>
public sealed class ChangePasswordCommandHandler(IAuthService auth, ICurrentUser currentUser)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(ChangePasswordCommand c, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");
        return auth.ChangePasswordAsync(userId, c.CurrentPassword, c.NewPassword, ct);
    }
}
