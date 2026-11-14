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

    /// <summary>A member's membership of one school, tracked (so changes are saved), with the school.</summary>
    /// <param name="memberId">The member.</param>
    /// <param name="communityId">The school.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The membership, or null if they haven't joined it.</returns>
    Task<CommunityMembership?> GetAsync(Guid memberId, Guid communityId, CancellationToken cancellationToken = default);

    /// <summary>A member's schools, with each school and its other names, most recent years first.</summary>
    /// <param name="memberId">The member.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The memberships, read-only.</returns>
    Task<IReadOnlyList<CommunityMembership>> ListForMemberAsync(Guid memberId,
                                                                CancellationToken cancellationToken = default);
}
