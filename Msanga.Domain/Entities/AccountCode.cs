using System.Security.Cryptography;
using System.Text;
using Msanga.Domain.Enums;

namespace Msanga.Domain.Entities;

/// <summary>
/// One 6-digit code emailed to a member: "confirm your email" or "reset your password" (table AccountCodes). The
/// member types it into the page, so it works even when the email is read on another device. Msanga never emails
/// sign-in links.
/// </summary>
/// <remarks>
/// <para><b>Works once, for a short time, with few tries:</b> <see cref="IsUsable"/> is false once the code is used,
/// expired, or has had <see cref="MaxFailedAttempts"/> wrong tries. Sending a new code of the same kind retires the
/// older ones (see <c>IAccountCodeRepository</c>).</para>
/// <para><b>Only a hash is stored</b> (SHA-256 of the account's Id, the purpose and the code), never the code.
/// A 6-digit code has only a million values, so the hash alone wouldn't stop someone holding a stolen database;
/// what protects members is that a code lives minutes, works once and allows 5 tries.</para>
/// </remarks>
public sealed class AccountCode
{
    /// <summary>Length of a SHA-256 hash written as hex.</summary>
    public const int CodeHashLength = 64;

    /// <summary>How many digits a code has.</summary>
    public const int Digits = 6;

    /// <summary>Wrong tries allowed before the code stops working.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>Required by EF Core.</summary>
    private AccountCode() { }

    /// <summary>Records a newly emailed code.</summary>
    /// <param name="userId">The account it belongs to.</param>
    /// <param name="purpose">What it is for.</param>
    /// <param name="code">The 6 digits (only a hash is kept).</param>
    /// <param name="createdAtUtc">When it was made.</param>
    /// <param name="expiresAtUtc">When it stops working.</param>
    /// <exception cref="ArgumentException">Not 6 digits.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Expiry not after creation.</exception>
    public AccountCode(Guid userId, AccountCodePurpose purpose, string code, DateTime createdAtUtc,
                       DateTime expiresAtUtc)
    {
        if (code is null || code.Length != Digits || !code.All(char.IsAsciiDigit))
            throw new ArgumentException($"A code must be {Digits} digits.", nameof(code));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresAtUtc, createdAtUtc);

        Id = Guid.CreateVersion7();
        UserId = userId;
        Purpose = purpose;
        CodeHash = Hash(userId, purpose, code);
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Row Id.</summary>
    public Guid Id { get; private set; }

    /// <summary>The account this code belongs to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>What it is for.</summary>
    public AccountCodePurpose Purpose { get; private set; }

    /// <summary>SHA-256 of (account, purpose, code) as upper-case hex. The code itself is never stored.</summary>
    public string CodeHash { get; private set; } = string.Empty;

    /// <summary>When it was made (UTC).</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>When it stops working (UTC).</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>When it was used, retired by a newer code, or stopped after too many wrong tries (UTC); null while
    /// usable.</summary>
    public DateTime? UsedAtUtc { get; private set; }

    /// <summary>Wrong tries so far.</summary>
    public int FailedAttempts { get; private set; }

    /// <summary>True while it can still be used.</summary>
    /// <param name="utcNow">The current time.</param>
    public bool IsUsable(DateTime utcNow) =>
        UsedAtUtc is null && utcNow < ExpiresAtUtc && FailedAttempts < MaxFailedAttempts;

    /// <summary>
    /// Checks a typed code. Right: the code is used up. Wrong: one try is counted, and the code stops working at
    /// <see cref="MaxFailedAttempts"/>.
    /// </summary>
    /// <param name="typed">What the member typed (spaces and dashes already removed).</param>
    /// <param name="utcNow">The current time.</param>
    /// <returns>True if it matched and the code was usable.</returns>
    public bool TryUse(string typed, DateTime utcNow)
    {
        if (!IsUsable(utcNow)) return false;

        var matches = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(UserId, Purpose, typed)), Encoding.ASCII.GetBytes(CodeHash));
        if (matches)
        {
            UsedAtUtc = utcNow;
            return true;
        }

        FailedAttempts++;
        if (FailedAttempts >= MaxFailedAttempts) UsedAtUtc = utcNow;
        return false;
    }

    /// <summary>Retires the code (a newer one was sent). Twice keeps the first time.</summary>
    /// <param name="utcNow">The current time.</param>
    public void Retire(DateTime utcNow) => UsedAtUtc ??= utcNow;

    /// <summary>SHA-256 of (account, purpose, code) as upper-case hex.</summary>
    private static string Hash(Guid userId, AccountCodePurpose purpose, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{purpose}:{code}")));
}
