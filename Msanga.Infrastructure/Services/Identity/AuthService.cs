using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Msanga.Domain.Constants;
using Msanga.Domain.Entities;
using Msanga.Domain.Enums;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Interfaces.Identity;
using Msanga.Domain.Shared.Errors;
using Msanga.Domain.Shared.Results;
using Msanga.Infrastructure.Services.Email;
using Msanga.Infrastructure.Settings.Identity;
using Msanga.Persistence.Configurations.Identity;

namespace Msanga.Infrastructure.Services.Identity;

/// <summary>
/// <see cref="IAuthService"/> on ASP.NET Core Identity: accounts with confirmed emails, password sign-in (with
/// lockout), rotating refresh tokens, emailed 6-digit codes, and password reset / change.
/// </summary>
/// <remarks>
/// <para><b>Privacy:</b> see <see cref="IAuthService"/>. In particular, the password is checked against the rules
/// BEFORE looking the email up, so the answer to Create account never depends on whether the email is taken.</para>
/// <para><b>Refresh tokens rotate:</b> every refresh revokes the old token. If a revoked token is used again, it was
/// copied, so every session of that account is ended.</para>
/// <para><b>Emails</b> go out after the database is saved; a failure is logged, never shown (the person can ask for
/// a new code).</para>
/// <para><b>Codes:</b> a wrong code, an expired one and an unknown email all get the same <c>InvalidCode</c>, so the
/// answer never says whether the email has an account. Each code allows <c>AccountCode.MaxFailedAttempts</c> tries.</para>
/// <para>Identity's UserManager and the repositories share one DbContext, so a single save covers both.</para>
/// </remarks>
internal sealed class AuthService(
    UserManager<ApplicationUser> users,
    IMemberRepository members,
    IRefreshTokenRepository refreshTokens,
    IAccountCodeRepository codes,
    IUnitOfWork unitOfWork,
    JwtTokenGenerator tokenGenerator,
    AccountEmails emails,
    IOptions<IdentitySettings> settings,
    TimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    // ───────────────────────────── 1. Create account ─────────────────────────────

    /// <inheritdoc />
    public async Task<Result> RegisterAsync(NewAccount account, CancellationToken ct = default)
    {
        var email = account.Email.Trim();
        var user = new ApplicationUser { UserName = email, Email = email };

        // 1. Password rules first, so the answer is the same whether or not the email is taken
        var weak = await CheckPasswordRulesAsync(user, account.Password);
        if (weak is not null) return weak;

        // 2. Taken: answer as if it worked, and email the owner: a fresh confirmation code if they never confirmed
        //    (they probably lost the first email), else "you already have an account"
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null && !existing.EmailConfirmed)
            return await ResendConfirmationAsync(email, ct);
        if (existing is not null)
        {
            var owner = await members.GetByIdAsync(existing.Id, ct);
            await TrySendAsync("already-registered", existing.Id,
                () => emails.SendAlreadyRegisteredAsync(existing.Email!, owner?.FirstName ?? "there", ct));
            return Result.Success();
        }

        // 3. Account + Member role + member + confirmation code, all or nothing
        var member = Member.Create(user.Id, account.FirstName, account.LastName, account.SchoolSurname);
        string code;
        await using (var transaction = await unitOfWork.BeginTransactionAsync(ct))
        {
            var created = await users.CreateAsync(user, account.Password);                  // saves
            if (!created.Succeeded)
            {
                // Created by another request a moment ago: same answer as "taken"
                if (created.Errors.Any(e => e.Code is nameof(IdentityErrorDescriber.DuplicateEmail)
                                                   or nameof(IdentityErrorDescriber.DuplicateUserName)))
                    return Result.Success();
                return AuthErrors.WeakPassword(Describe(created));
            }

            var role = await users.AddToRoleAsync(user, Roles.Member);                      // saves
            if (!role.Succeeded)
                throw new InvalidOperationException($"Could not give the new account the Member role: {Describe(role)}");

            members.Add(member);
            code = await NewCodeAsync(user.Id, AccountCodePurpose.ConfirmEmail, ct);
            await unitOfWork.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        // 4. The email, after everything is saved
        await TrySendAsync("confirm-email", user.Id,
            () => emails.SendConfirmEmailAsync(email, member.FirstName, code, ct));
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ConfirmEmailAsync(string email, string code, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(email.Trim());
        if (user is null) return AuthErrors.InvalidCode;
        // Already confirmed: the same answer as a wrong code, so this can't be used to find confirmed accounts
        // (the web page says "already confirmed? just sign in" next to the message)
        if (user.EmailConfirmed) return AuthErrors.InvalidCode;

        if (!await TryUseCodeAsync(user.Id, AccountCodePurpose.ConfirmEmail, code, ct)) return AuthErrors.InvalidCode;

        user.EmailConfirmed = true;
        var updated = await users.UpdateAsync(user);                                        // saves the code too
        if (!updated.Succeeded)
            throw new InvalidOperationException($"Could not confirm the email: {Describe(updated)}");

        await unitOfWork.SaveChangesAsync(ct);                                              // anything left
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResendConfirmationAsync(string email, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(email.Trim());
        if (user is null || user.EmailConfirmed) return Result.Success();

        var member = await members.GetByIdAsync(user.Id, ct);
        var code = await NewCodeAsync(user.Id, AccountCodePurpose.ConfirmEmail, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await TrySendAsync("confirm-email", user.Id,
            () => emails.SendConfirmEmailAsync(user.Email!, member?.FirstName ?? "there", code, ct));
        return Result.Success();
    }

    // ───────────────────────────── 2. Sign in / out ─────────────────────────────

    /// <inheritdoc />
    public async Task<Result<AuthTokens>> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(email.Trim());
        if (user is null) return AuthErrors.InvalidCredentials;

        var passwordCheck = await CheckPasswordAsync(user, password);
        if (passwordCheck.IsFailure) return passwordCheck.Error;

        // Only said to someone who knows the password
        if (!user.EmailConfirmed) return AuthErrors.EmailNotConfirmed;

        var account = await AccountAsync(user, ct);
        if (account is null) return AuthErrors.UserNotFound;

        var tokens = StartSession(user, await users.GetRolesAsync(user), account, isNewSignIn: true).Tokens;
        await unitOfWork.SaveChangesAsync(ct);
        return tokens;
    }

    /// <inheritdoc />
    public async Task<Result<AuthTokens>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var now = Now;
        var stored = await refreshTokens.GetByTokenAsync(refreshToken, ct);
        if (stored is null) return AuthErrors.InvalidToken;

        // A revoked token used again: it was copied. End every session of this account.
        if (stored.RevokedAtUtc is not null)
        {
            logger.LogWarning("A revoked refresh token was used again for user {UserId}; ending all their sessions.",
                              stored.UserId);
            await RevokeAllAsync(stored.UserId, now, ct);
            await unitOfWork.SaveChangesAsync(ct);
            return AuthErrors.InvalidToken;
        }

        if (!stored.IsActive(now)) return AuthErrors.InvalidToken;   // expired

        var user = await users.FindByIdAsync(stored.UserId.ToString());
        var account = user is null ? null : await AccountAsync(user, ct);
        if (user is null || account is null)
        {
            stored.Revoke(now);
            await unitOfWork.SaveChangesAsync(ct);
            return AuthErrors.InvalidToken;
        }

        // Rotate: a new pair (with the account's CURRENT roles); the old token points at its replacement
        var session = StartSession(user, await users.GetRolesAsync(user), account, isNewSignIn: false);
        stored.Revoke(now, session.Row.Id);
        await unitOfWork.SaveChangesAsync(ct);
        return session.Tokens;
    }

    /// <inheritdoc />
    /// <remarks>Signing out twice, or with an unknown token, still succeeds.</remarks>
    public async Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await refreshTokens.GetByTokenAsync(refreshToken, ct);
        if (stored is null) return Result.Success();

        stored.Revoke(Now);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ───────────────────────────── 3. Passwords ─────────────────────────────

    /// <inheritdoc />
    public async Task<Result> RequestPasswordResetAsync(string email, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(email.Trim());
        if (user is null) return Result.Success();

        var member = await members.GetByIdAsync(user.Id, ct);
        var code = await NewCodeAsync(user.Id, AccountCodePurpose.ResetPassword, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await TrySendAsync("reset-password", user.Id,
            () => emails.SendResetPasswordAsync(user.Email!, member?.FirstName ?? "there", code, ct));
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(string email, string code, string newPassword,
                                                 CancellationToken ct = default)
    {
        var now = Now;

        // 1. Password rules first (before looking the email up, so the answer doesn't depend on whether it has an
        //    account); a weak password doesn't use up the code or a try, so they can choose another
        var weak = await CheckPasswordRulesAsync(new ApplicationUser { UserName = email.Trim(), Email = email.Trim() },
                                                 newPassword);
        if (weak is not null) return weak;

        // 2. The code
        var user = await users.FindByEmailAsync(email.Trim());
        if (user is null) return AuthErrors.InvalidCode;

        if (!await TryUseCodeAsync(user.Id, AccountCodePurpose.ResetPassword, code, ct)) return AuthErrors.InvalidCode;

        user.PasswordHash = users.PasswordHasher.HashPassword(user, newPassword);
        user.EmailConfirmed = true;          // they've just proved they can read the inbox
        user.AccessFailedCount = 0;          // a lockout from guessing ends with the reset
        user.LockoutEnd = null;
        await RevokeAllAsync(user.Id, now, ct);

        var updated = await users.UpdateSecurityStampAsync(user);                           // saves everything
        if (!updated.Succeeded)
            throw new InvalidOperationException($"Could not reset the password: {Describe(updated)}");
        await unitOfWork.SaveChangesAsync(ct);

        await NotifyPasswordChangedAsync(user, ct);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword,
                                                  CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null) return AuthErrors.UserNotFound;

        var changed = await users.ChangePasswordAsync(user, currentPassword, newPassword);   // saves
        if (!changed.Succeeded)
            return changed.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch))
                ? AuthErrors.WrongPassword
                : AuthErrors.WeakPassword(Describe(changed));

        // Everyone signed in with the old password is signed out (including other devices)
        await RevokeAllAsync(user.Id, Now, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await NotifyPasswordChangedAsync(user, ct);
        return Result.Success();
    }

    // ───────────────────────────── 4. Account ─────────────────────────────

    /// <inheritdoc />
    public async Task<Result<MemberAccount>> GetAccountAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        var account = user is null ? null : await AccountAsync(user, ct);
        if (account is null) return AuthErrors.UserNotFound;
        return account;
    }

    // ───────────────────────────── Helpers ─────────────────────────────

    /// <summary>The current time (UTC), from the app's clock.</summary>
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    /// <summary>The member with their email; null if the member row is missing.</summary>
    private async Task<MemberAccount?> AccountAsync(ApplicationUser user, CancellationToken ct)
    {
        var member = await members.GetByIdAsync(user.Id, ct);
        return member is null
            ? null
            : new MemberAccount(user.Id, user.Email ?? string.Empty, member.FirstName, member.LastName,
                                member.SchoolSurname, member.DisplayName, member.Bio, member.PhotoUrl);
    }

    /// <summary>Checks a new password against Identity's rules (length etc.); null when it passes.</summary>
    private async Task<Error?> CheckPasswordRulesAsync(ApplicationUser user, string password)
    {
        var problems = new List<IdentityError>();
        foreach (var validator in users.PasswordValidators)
        {
            var result = await validator.ValidateAsync(users, user, password);
            if (!result.Succeeded) problems.AddRange(result.Errors);
        }
        return problems.Count == 0 ? null : AuthErrors.WeakPassword(string.Join(" ", problems.Select(e => e.Description)));
    }

    /// <summary>Checks the password, counting failures towards the lockout.</summary>
    private async Task<Result> CheckPasswordAsync(ApplicationUser user, string password)
    {
        if (await users.IsLockedOutAsync(user)) return AuthErrors.LockedOut;

        if (await users.CheckPasswordAsync(user, password))
        {
            if (user.AccessFailedCount > 0) await users.ResetAccessFailedCountAsync(user);
            return Result.Success();
        }

        await users.AccessFailedAsync(user);
        return await users.IsLockedOutAsync(user) ? AuthErrors.LockedOut : AuthErrors.InvalidCredentials;
    }

    /// <summary>
    /// Makes a new emailed code: retires the account's older codes of the same kind, adds the new one for saving
    /// (NOT saved here) and returns the 6 digits for the email.
    /// </summary>
    private async Task<string> NewCodeAsync(Guid userId, AccountCodePurpose purpose, CancellationToken ct)
    {
        var now = Now;
        foreach (var older in await codes.GetUsableForUserAsync(userId, purpose, now, ct))
            older.Retire(now);

        var lifetime = TimeSpan.FromMinutes(purpose == AccountCodePurpose.ConfirmEmail
            ? settings.Value.Codes.ConfirmEmailMinutes
            : settings.Value.Codes.ResetPasswordMinutes);

        // 000000–999999 from the cryptographic random generator (never Random: it can be predicted)
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        codes.Add(new AccountCode(userId, purpose, code, now, now + lifetime));
        return code;
    }

    /// <summary>
    /// Checks a typed code against the account's newest working code of that kind. Right: it's used up. Wrong: a
    /// try is counted (saved at once, so the 5-try limit holds even if nothing else is saved).
    /// </summary>
    /// <returns>True if the code was right.</returns>
    private async Task<bool> TryUseCodeAsync(Guid userId, AccountCodePurpose purpose, string typed, CancellationToken ct)
    {
        var now = Now;
        var current = (await codes.GetUsableForUserAsync(userId, purpose, now, ct)).FirstOrDefault();
        if (current is null) return false;

        if (current.TryUse(typed, now)) return true;       // saved by the caller, with the rest of the change

        await unitOfWork.SaveChangesAsync(ct);              // the wrong try counts
        return false;
    }

    /// <summary>
    /// Makes a new access + refresh pair and adds the refresh token for saving. Does NOT save: the caller may have
    /// more to change first (e.g. revoking the old token).
    /// </summary>
    private Session StartSession(ApplicationUser user, IList<string> roles, MemberAccount account, bool isNewSignIn)
    {
        var access = tokenGenerator.CreateAccessToken(user, roles);
        var refresh = tokenGenerator.CreateRefreshToken();

        var row = new RefreshToken(user.Id, refresh.Value, refresh.IssuedAtUtc, refresh.ExpiresAtUtc);
        refreshTokens.Add(row);

        if (isNewSignIn) user.LastLoginAt = access.IssuedAtUtc;

        var tokens = new AuthTokens(access.Value, access.ExpiresAtUtc, refresh.Value, refresh.ExpiresAtUtc, account);
        return new Session(tokens, row);
    }

    /// <summary>Revokes every working refresh token of an account (not saved).</summary>
    private async Task RevokeAllAsync(Guid userId, DateTime now, CancellationToken ct)
    {
        foreach (var token in await refreshTokens.GetActiveForUserAsync(userId, now, ct))
            token.Revoke(now);
    }

    /// <summary>"Your password was changed", to the account's own address.</summary>
    private async Task NotifyPasswordChangedAsync(ApplicationUser user, CancellationToken ct)
    {
        var member = await members.GetByIdAsync(user.Id, ct);
        await TrySendAsync("password-changed", user.Id,
            () => emails.SendPasswordChangedAsync(user.Email!, member?.FirstName ?? "there", ct));
    }

    /// <summary>Sends an email; a failure is logged (with the account's Id, never the address or code) and swallowed.</summary>
    private async Task TrySendAsync(string kind, Guid userId, Func<Task> send)
    {
        try
        {
            await send();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "The {EmailKind} email for user {UserId} could not be sent.", kind, userId);
        }
    }

    private static string Describe(IdentityResult result) => string.Join(" ", result.Errors.Select(e => e.Description));

    /// <summary>A new session: the tokens to hand out and the refresh-token row to save.</summary>
    private sealed record Session(AuthTokens Tokens, RefreshToken Row);
}
