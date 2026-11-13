using Msanga.Domain.Entities;
using Msanga.Domain.Enums;

namespace Msanga.Domain.Interfaces.Identity;

/// <summary>
/// Loads and saves <see cref="AccountCode"/>s (table AccountCodes). Used only by the Identity services in
/// Infrastructure. Never saves: the caller saves once.
/// </summary>
public interface IAccountCodeRepository
{
    /// <summary>Marks a newly emailed code to be inserted on the next save.</summary>
    /// <param name="code">The new code.</param>
    void Add(AccountCode code);

    /// <summary>
    /// An account's codes of one kind that still work, newest first. Tracked: the newest is checked against what
    /// the member typed, and all of them are retired when a new one is sent.
    /// </summary>
    /// <param name="userId">The account.</param>
    /// <param name="purpose">The kind of code.</param>
    /// <param name="utcNow">The current time (expired codes are left out).</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<IReadOnlyList<AccountCode>> GetUsableForUserAsync(Guid userId, AccountCodePurpose purpose, DateTime utcNow,
                                                           CancellationToken cancellationToken = default);
}
