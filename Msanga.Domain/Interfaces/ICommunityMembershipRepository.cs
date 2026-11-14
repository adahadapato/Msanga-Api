using Msanga.Domain.Entities;

namespace Msanga.Domain.Interfaces;

/// <summary>Who joined which school. Never saves: the caller saves once (<see cref="IUnitOfWork"/>).</summary>
public interface ICommunityMembershipRepository
{
    /// <summary>Marks a new membership to be inserted on the next save.</summary>
    /// <param name="membership">The membership.</param>
    void Add(CommunityMembership membership);

    /// <summary>Marks a membership to be deleted on the next save (leaving a school).</summary>
    /// <param name="membership">The membership.</param>
    void Remove(CommunityMembership membership);

    /// <summary>A member's membership of one school, tracked (so changes are saved), with the member and the school.</summary>
    /// <param name="memberId">The member.</param>
    /// <param name="communityId">The school.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The membership, or null if they haven't joined it.</returns>
    Task<CommunityMembership?> GetAsync(Guid memberId, Guid communityId, CancellationToken cancellationToken = default);

    /// <summary>How many confirmed members a school has (its early days end at 3, <c>Rules.Vouching</c>).</summary>
    /// <param name="communityId">The school.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The number.</returns>
    Task<int> CountConfirmedAsync(Guid communityId, CancellationToken cancellationToken = default);

    /// <summary>People waiting to be confirmed at a school (Joined), with each member, by set then name.</summary>
    /// <param name="communityId">The school.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The memberships, read-only.</returns>
    Task<IReadOnlyList<CommunityMembership>> ListWaitingAsync(Guid communityId,
                                                              CancellationToken cancellationToken = default);

    /// <summary>A set's confirmed members, with each member, by name.</summary>
    /// <param name="communityId">The school.</param>
    /// <param name="setYear">The set.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The memberships, read-only.</returns>
    Task<IReadOnlyList<CommunityMembership>> ListConfirmedInSetAsync(Guid communityId, int setYear,
                                                                     CancellationToken cancellationToken = default);

    /// <summary>A member's schools, with each school and its other names, most recent years first.</summary>
    /// <param name="memberId">The member.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The memberships, read-only.</returns>
    Task<IReadOnlyList<CommunityMembership>> ListForMemberAsync(Guid memberId,
                                                                CancellationToken cancellationToken = default);
}
