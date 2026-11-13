using Msanga.Domain.Entities;
using Msanga.Domain.Enums;

namespace Msanga.Domain.Interfaces.Identity;

/// <summary>
/// Loads and saves <see cref="AccountLink"/>s (table AccountLinks). Used only by the Identity services in
/// Infrastructure. Never saves: the caller saves once.
/// </summary>
public interface IAccountLinkRepository
{
    /// <summary>Marks a newly emailed link to be inserted on the next save.</summary>
    /// <param name="link">The new link.</param>
    void Add(AccountLink link);

    /// <summary>Finds a link by the raw token from the email (hashed here, then looked up). Tracked.</summary>
    /// <param name="token">The raw token.</param>
    /// <param name="purpose">What the link must be for (a reset token can't confirm an email, and so on).</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The link, or null (it may still be used or expired: check <c>IsUsable</c>).</returns>
    Task<AccountLink?> GetByTokenAsync(string token, AccountLinkPurpose purpose,
                                       CancellationToken cancellationToken = default);

    /// <summary>An account's links of one kind that still work. Tracked: used to retire them when a new one is sent.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="purpose">The kind of link.</param>
    /// <param name="utcNow">The current time (expired links are left out).</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<IReadOnlyList<AccountLink>> GetUsableForUserAsync(Guid userId, AccountLinkPurpose purpose, DateTime utcNow,
                                                           CancellationToken cancellationToken = default);
}
