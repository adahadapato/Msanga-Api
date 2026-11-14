using Microsoft.EntityFrameworkCore;
using Msanga.Domain.Entities;
using Msanga.Domain.Enums;
using Msanga.Domain.Interfaces;
using Msanga.Domain.ValueObjects;

namespace Msanga.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ICommunityMembershipRepository"/>.</summary>
internal sealed class CommunityMembershipRepository(MsangaDbContext context) : ICommunityMembershipRepository
{
    /// <inheritdoc />
    public void Add(CommunityMembership membership) => context.CommunityMemberships.Add(membership);

    /// <inheritdoc />
    public void Remove(CommunityMembership membership) => context.CommunityMemberships.Remove(membership);

    /// <inheritdoc />
    public Task<CommunityMembership?> GetAsync(Guid memberId, Guid communityId,
                                               CancellationToken cancellationToken = default) =>
        context.CommunityMemberships
               .Include(m => m.Member)
               .Include(m => m.Community).ThenInclude(c => c.Aliases)
               .FirstOrDefaultAsync(m => m.MemberId == memberId && m.CommunityId == communityId, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountConfirmedAsync(Guid communityId, CancellationToken cancellationToken = default) =>
        context.CommunityMemberships.CountAsync(
            m => m.CommunityId == communityId && m.Status == CommunityMembershipStatus.Confirmed, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommunityMembership>> ListWaitingAsync(
        Guid communityId, CancellationToken cancellationToken = default) =>
        await context.CommunityMemberships.AsNoTracking()
                     .Include(m => m.Member)
                     .Where(m => m.CommunityId == communityId && m.Status == CommunityMembershipStatus.Joined)
                     .OrderBy(m => m.SetYear ?? m.ToYear).ThenBy(m => m.Member.FirstName).ThenBy(m => m.Member.LastName)
                     .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommunityMembership>> ListConfirmedInSetAsync(
        Guid communityId, int setYear, CancellationToken cancellationToken = default) =>
        await context.CommunityMemberships.AsNoTracking()
                     .Include(m => m.Member)
                     .Where(m => m.CommunityId == communityId && m.SetYear == setYear
                              && m.Status == CommunityMembershipStatus.Confirmed)
                     .OrderBy(m => m.Member.FirstName).ThenBy(m => m.Member.LastName)
                     .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> ConfirmedSchoolIdsAsync(Guid memberId,
                                                                  CancellationToken cancellationToken = default) =>
        await context.CommunityMemberships.AsNoTracking()
                     .Where(m => m.MemberId == memberId && m.Status == CommunityMembershipStatus.Confirmed)
                     .Select(m => m.CommunityId)
                     .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommunityMembership>> FindPeopleAsync(
        PeopleFilter filter, IReadOnlyCollection<Guid> schoolIds, Guid excludeMemberId, int maxPeople,
        CancellationToken cancellationToken = default)
    {
        var matches = context.CommunityMemberships.AsNoTracking()
            .Where(m => schoolIds.Contains(m.CommunityId) && m.Status == CommunityMembershipStatus.Confirmed
                     && m.MemberId != excludeMemberId);

        if (filter.SchoolId is { } school) matches = matches.Where(m => m.CommunityId == school);
        if (filter.SetYear is { } set) matches = matches.Where(m => m.SetYear == set);
        if (!string.IsNullOrWhiteSpace(filter.House))
        {
            var house = filter.House.Trim();
            matches = matches.Where(m => m.House != null && m.House.Contains(house));
        }
        if (filter.CountryCode is { } country) matches = matches.Where(m => m.Member.LivesInCountryCode == country);

        // Each word of the name in the first name, last name or surname at school (SQL Server ignores case)
        foreach (var word in filter.NameWords)
        {
            matches = matches.Where(m => m.Member.FirstName.Contains(word) || m.Member.LastName.Contains(word)
                                      || (m.Member.SchoolSurname != null && m.Member.SchoolSurname.Contains(word)));
        }

        var people = await matches
            .Select(m => new { m.MemberId, m.Member.FirstName, m.Member.LastName })
            .Distinct()
            .OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
            .Take(maxPeople)
            .Select(p => p.MemberId)
            .ToListAsync(cancellationToken);

        return await context.CommunityMemberships.AsNoTracking()
            .Include(m => m.Member)
            .Include(m => m.Community)
            .Where(m => people.Contains(m.MemberId) && schoolIds.Contains(m.CommunityId)
                     && m.Status == CommunityMembershipStatus.Confirmed)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommunityMembership>> ConfirmedAtAsync(
        Guid memberId, IReadOnlyCollection<Guid> schoolIds, CancellationToken cancellationToken = default) =>
        await context.CommunityMemberships.AsNoTracking()
                     .Include(m => m.Member)
                     .Include(m => m.Community)
                     .Where(m => m.MemberId == memberId && schoolIds.Contains(m.CommunityId)
                              && m.Status == CommunityMembershipStatus.Confirmed)
                     .OrderBy(m => m.Community.Name)
                     .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommunityMembership>> ListForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default) =>
        await context.CommunityMemberships.AsNoTracking()
                     .Include(m => m.Community).ThenInclude(c => c.Aliases)
                     .Where(m => m.MemberId == memberId)
                     .OrderByDescending(m => m.ToYear)
                     .ThenBy(m => m.Community.Name)
                     .ToListAsync(cancellationToken);
}
