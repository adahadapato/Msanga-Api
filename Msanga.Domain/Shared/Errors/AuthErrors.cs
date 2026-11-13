namespace Msanga.Domain.Shared.Errors;

/// <summary>
/// Account and sign-in failures returned by <c>IAuthService</c>, and written by Presentation when a request is
/// refused. Messages are written for the person using the app; the web app can also switch on the code.
/// </summary>
/// <remarks>
/// Privacy: nothing here says whether an email has an account. A wrong email and a wrong password get the same
/// <see cref="InvalidCredentials"/>; a wrong code and an unknown email get the same <see cref="InvalidCode"/>;
/// registering with a taken email, and "forgot password", always look like success.
/// </remarks>
public static class AuthErrors
{
    // ---------- Sign-in ----------

    /// <summary>Deliberately vague: never reveal whether the email exists.</summary>
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Auth.InvalidCredentials", "The email or password is not right. Check them and try again.");

    /// <summary>The right password, but the email isn't confirmed yet.</summary>
    public static readonly Error EmailNotConfirmed = Error.Forbidden(
        "Auth.EmailNotConfirmed", "Please confirm your email first: enter the code we sent you.");

    /// <summary>Too many wrong passwords in a row.</summary>
    public static readonly Error LockedOut = Error.Forbidden(
        "Auth.LockedOut", "Too many attempts. Please wait a few minutes and try again.");

    /// <summary>The refresh token is unknown, revoked or expired.</summary>
    public static readonly Error InvalidToken = Error.Unauthorized(
        "Auth.InvalidToken", "Your session has ended. Please sign in again.");

    // ---------- Emailed codes ----------

    /// <summary>
    /// A confirm-email or reset-password code that is wrong, expired, used, or stopped after 5 wrong tries. One
    /// message for all of them, so the answer never says whether the email has an account.
    /// </summary>
    public static readonly Error InvalidCode = Error.Validation(
        "Auth.InvalidCode", "That code isn't right or has expired. Check it, or ask for a new code.");

    // ---------- Account ----------

    /// <summary>Changing the password: the current one is wrong.</summary>
    public static readonly Error WrongPassword = Error.Validation(
        "Auth.WrongPassword", "Your current password isn't right.");

    /// <summary>The new password breaks the password rules.</summary>
    /// <param name="reasons">Identity's descriptions, joined.</param>
    /// <returns>The error.</returns>
    public static Error WeakPassword(string reasons) => Error.Validation("Auth.WeakPassword", reasons);

    /// <summary>The signed-in account no longer exists.</summary>
    public static readonly Error UserNotFound = Error.NotFound(
        "Auth.UserNotFound", "That account wasn't found.");

    // ---------- Access (written by Presentation when a request is refused) ----------

    /// <summary>No access token sent.</summary>
    public static readonly Error SignInRequired = Error.Unauthorized(
        "Auth.SignInRequired", "Please sign in to do this.");

    /// <summary>The access token has expired: refresh it, or sign in again.</summary>
    public static readonly Error SessionExpired = Error.Unauthorized(
        "Auth.SessionExpired", "Your session has expired. Please sign in again.");

    /// <summary>The access token is damaged, forged or for another app.</summary>
    public static readonly Error InvalidAccessToken = Error.Unauthorized(
        "Auth.InvalidAccessToken", "Your sign-in isn't valid. Please sign in again.");

    /// <summary>Signed in, but this needs a role the user doesn't have.</summary>
    public static readonly Error NotAllowed = Error.Forbidden(
        "Auth.NotAllowed", "You don't have permission to do this.");
}
