using Msanga.Domain.Entities;
using Msanga.Domain.ValueObjects;

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

    /// <summary>The schools where a member is confirmed: the only schools they can search (Find People).</summary>
    /// <param name="memberId">The member.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The school Ids.</returns>
    Task<IReadOnlyList<Guid>> ConfirmedSchoolIdsAsync(Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find People: up to <paramref name="maxPeople"/> people confirmed at any of <paramref name="schoolIds"/> who match
    /// the filter (A–Z by name), never <paramref name="excludeMemberId"/>. Returns their confirmed memberships at those
    /// schools (with member and school), so each person can be shown with every school you share.
    /// </summary>
    /// <param name="filter">What to look for.</param>
    /// <param name="schoolIds">The searcher's confirmed schools.</param>
    /// <param name="excludeMemberId">The searcher.</param>
    /// <param name="maxPeople">The most people.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The memberships, read-only.</returns>
    Task<IReadOnlyList<CommunityMembership>> FindPeopleAsync(PeopleFilter filter, IReadOnlyCollection<Guid> schoolIds,
                                                             Guid excludeMemberId, int maxPeople,
                                                             CancellationToken cancellationToken = default);

    /// <summary>A member's confirmed memberships at some schools (a person's page: the schools you share), with
    /// member and school.</summary>
    /// <param name="memberId">The person.</param>
    /// <param name="schoolIds">The viewer's confirmed schools.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The memberships, read-only (none = you share no school: "not found").</returns>
    Task<IReadOnlyList<CommunityMembership>> ConfirmedAtAsync(Guid memberId, IReadOnlyCollection<Guid> schoolIds,
                                                              CancellationToken cancellationToken = default);

    /// <summary>A member's schools, with each school and its other names, most recent years first.</summary>
    /// <param name="memberId">The member.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The memberships, read-only.</returns>
    Task<IReadOnlyList<CommunityMembership>> ListForMemberAsync(Guid memberId,
                                                                CancellationToken cancellationToken = default);
}
