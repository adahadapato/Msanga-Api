using Msanga.Domain.Shared.Results;

namespace Msanga.Domain.Interfaces;

/// <summary>
/// Accounts and sign-in: register, confirm email, sign in, tokens, forgot / reset / change password. Implemented in
/// Infrastructure with ASP.NET Core Identity + JWT. Failures are returned as <c>AuthErrors</c>, never thrown.
/// </summary>
/// <remarks>
/// <para><b>Privacy:</b> nothing here tells a stranger whether an email has an account. Registering with a taken
/// email succeeds (the owner gets an email saying so); resending a confirmation and "forgot password" always
/// succeed; a wrong email and a wrong password give the same error, and so do a wrong code and an unknown email.</para>
/// <para><b>Codes, not links:</b> confirming an email and resetting a password use a 6-digit code from the email,
/// typed into the page (works across devices, and Msanga never emails sign-in links).</para>
/// <para><b>Emails</b> are sent by the service itself. If sending fails, it is logged and the call still succeeds:
/// the person can ask for a new code.</para>
/// </remarks>
public interface IAuthService
{
    /// <summary>Creates an account (and its member) and emails the confirmation code.</summary>
    /// <param name="account">The details from the form.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>Success (also when the email is taken: see remarks); or WeakPassword.</returns>
    Task<Result> RegisterAsync(NewAccount account, CancellationToken ct = default);

    /// <summary>Confirms the email with the code from the email (the code then stops working).</summary>
    /// <param name="email">The account's email.</param>
    /// <param name="code">The 6 digits typed by the member.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>Success; or InvalidCode.</returns>
    Task<Result> ConfirmEmailAsync(string email, string code, CancellationToken ct = default);

    /// <summary>Emails a new confirmation code, if the email has an account that isn't confirmed yet.</summary>
    /// <param name="email">The address.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>Always success.</returns>
    Task<Result> ResendConfirmationAsync(string email, CancellationToken ct = default);

    /// <summary>Checks email + password and issues an access + refresh pair.</summary>
    /// <param name="email">The account's email.</param>
    /// <param name="password">The password.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>The tokens; or InvalidCredentials, LockedOut or EmailNotConfirmed.</returns>
    Task<Result<AuthTokens>> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Swaps a valid refresh token for a new pair (the old one stops working).</summary>
    /// <param name="refreshToken">The refresh token the app holds.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>The new tokens; or InvalidToken.</returns>
    Task<Result<AuthTokens>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Sign out: the refresh token can't be used again. Unknown tokens are ignored.</summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="ct">Cancels the database calls.</param>
    Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Emails a reset-password code, if the email has an account.</summary>
    /// <param name="email">The address.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>Always success.</returns>
    Task<Result> RequestPasswordResetAsync(string email, CancellationToken ct = default);

    /// <summary>Sets a new password with a reset code; signs out every session. Also confirms the email.</summary>
    /// <param name="email">The account's email.</param>
    /// <param name="code">The 6 digits typed by the member.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>Success; or InvalidCode or WeakPassword (a weak password doesn't use up the code).</returns>
    Task<Result> ResetPasswordAsync(string email, string code, string newPassword, CancellationToken ct = default);

    /// <summary>Changes the password and signs out every session.</summary>
    /// <param name="userId">From the access token.</param>
    /// <param name="currentPassword">The password now.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>Success; or UserNotFound, WrongPassword or WeakPassword.</returns>
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword,
                                     CancellationToken ct = default);

    /// <summary>Who the signed-in member is.</summary>
    /// <param name="userId">From the access token.</param>
    /// <param name="ct">Cancels the database calls.</param>
    /// <returns>The account; or UserNotFound.</returns>
    Task<Result<MemberAccount>> GetAccountAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>A new account, as typed into the Create account form.</summary>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="SchoolSurname">Surname at school, if different now; blank = none.</param>
/// <param name="Email">Email address (also the sign-in name).</param>
/// <param name="Password">The chosen password.</param>
public sealed record NewAccount(string FirstName, string LastName, string? SchoolSurname, string Email, string Password);

/// <summary>A signed-in session: short-lived access token + long-lived refresh token.</summary>
/// <param name="AccessToken">The JWT to send as "Authorization: Bearer …".</param>
/// <param name="AccessTokenExpiresAt">When it stops working (UTC).</param>
/// <param name="RefreshToken">Swaps for a new pair at <c>/api/auth/refresh</c>.</param>
/// <param name="RefreshTokenExpiresAt">When it stops working (UTC).</param>
/// <param name="Account">Who is signed in.</param>
public sealed record AuthTokens(
    string AccessToken, DateTime AccessTokenExpiresAt,
    string RefreshToken, DateTime RefreshTokenExpiresAt,
    MemberAccount Account);

/// <summary>A member with their sign-in email: what the app shows about the signed-in person.</summary>
/// <param name="Id">The member's (and account's) Id.</param>
/// <param name="Email">Their email (only they see it).</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="SchoolSurname">Surname at school; null = none.</param>
/// <param name="DisplayName">"Hilary A.".</param>
/// <param name="Bio">About them; empty = none.</param>
/// <param name="PhotoUrl">Their picture; null = initials.</param>
public sealed record MemberAccount(
    Guid Id, string Email, string FirstName, string LastName, string? SchoolSurname, string DisplayName,
    string Bio, string? PhotoUrl);
