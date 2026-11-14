using Msanga.Domain.Entities;

namespace Msanga.Domain.Interfaces;

/// <summary>"I know them": who vouched for whom at which school. Never saves: the caller saves once.</summary>
public interface IVouchRepository
{
    /// <summary>Marks a new vouch to be inserted on the next save.</summary>
    /// <param name="vouch">The vouch.</param>
    void Add(Vouch vouch);

    /// <summary>Has this member already vouched for that one at this school?</summary>
    /// <param name="communityId">The school.</param>
    /// <param name="voucherId">Who would vouch.</param>
    /// <param name="voucheeId">For whom.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>True if they have.</returns>
    Task<bool> ExistsAsync(Guid communityId, Guid voucherId, Guid voucheeId, CancellationToken cancellationToken = default);

    /// <summary>How many vouches someone has at a school (saved ones only).</summary>
    /// <param name="communityId">The school.</param>
    /// <param name="voucheeId">The person.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The number of vouches.</returns>
    Task<int> CountForAsync(Guid communityId, Guid voucheeId, CancellationToken cancellationToken = default);

    /// <summary>Who vouched for someone at a school, oldest first, as their short names ("Nse U."): "Confirmed by…".</summary>
    /// <param name="communityId">The school.</param>
    /// <param name="voucheeId">The person.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The names.</returns>
    Task<IReadOnlyList<string>> VoucherNamesAsync(Guid communityId, Guid voucheeId,
                                                 CancellationToken cancellationToken = default);

    /// <summary>The people a member has vouched for at a school ("You know them ✓" on the waiting list).</summary>
    /// <param name="communityId">The school.</param>
    /// <param name="voucherId">The member.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>Their member Ids.</returns>
    Task<IReadOnlySet<Guid>> VoucheesOfAsync(Guid communityId, Guid voucherId,
                                            CancellationToken cancellationToken = default);
}
