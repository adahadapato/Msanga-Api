using Msanga.Domain.Enums;

namespace Msanga.Domain.Entities;

/// <summary>
/// One link emailed to a member: "confirm your email" or "reset your password" (table AccountLinks). Like
/// <see cref="RefreshToken"/>, only a SHA-256 HASH of the token in the link is stored, so a leaked database can't be
/// used to take over an account.
/// </summary>
/// <remarks>
/// <para><b>Works once, for a limited time:</b> <see cref="Use"/> marks it used; <see cref="IsUsable"/> is false once
/// used or expired. Sending a new link of the same kind revokes the older ones (see <c>IAccountLinkRepository</c>).</para>
/// <para>Msanga's own links rather than Identity's built-in tokens: exact lifetimes per purpose, single use, and no
/// data-protection keys to keep safe between restarts and servers.</para>
/// </remarks>
public sealed class AccountLink
{
    /// <summary>Length of a SHA-256 hash written as hex.</summary>
    public const int TokenHashLength = RefreshToken.TokenHashLength;

    /// <summary>Required by EF Core.</summary>
    private AccountLink() { }

    /// <summary>Records a newly emailed link.</summary>
    /// <param name="userId">The account it belongs to.</param>
    /// <param name="purpose">What it does.</param>
    /// <param name="token">The raw token in the link (only its hash is kept).</param>
    /// <param name="createdAtUtc">When it was made.</param>
    /// <param name="expiresAtUtc">When it stops working.</param>
    /// <exception cref="ArgumentException">A blank token.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Expiry not after creation.</exception>
    public AccountLink(Guid userId, AccountLinkPurpose purpose, string token, DateTime createdAtUtc,
                       DateTime expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresAtUtc, createdAtUtc);

        Id = Guid.CreateVersion7();
        UserId = userId;
        Purpose = purpose;
        TokenHash = RefreshToken.Hash(token);
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Row Id.</summary>
    public Guid Id { get; private set; }

    /// <summary>The account this link belongs to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>What it does.</summary>
    public AccountLinkPurpose Purpose { get; private set; }

    /// <summary>SHA-256 of the token as upper-case hex. The raw token is never stored.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>When it was made (UTC).</summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>When it stops working (UTC).</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>When it was used, or replaced by a newer link (UTC); null while usable.</summary>
    public DateTime? UsedAtUtc { get; private set; }

    /// <summary>True while it can still be used.</summary>
    /// <param name="utcNow">The current time.</param>
    public bool IsUsable(DateTime utcNow) => UsedAtUtc is null && utcNow < ExpiresAtUtc;

    /// <summary>Uses up the link (or retires it when a newer one is sent). Twice keeps the first time.</summary>
    /// <param name="utcNow">The current time.</param>
    public void Use(DateTime utcNow) => UsedAtUtc ??= utcNow;
}
