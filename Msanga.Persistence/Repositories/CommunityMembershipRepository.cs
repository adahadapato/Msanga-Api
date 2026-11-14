using Microsoft.EntityFrameworkCore;
using Msanga.Domain.Entities;
using Msanga.Domain.Interfaces;

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
               .Include(m => m.Community).ThenInclude(c => c.Aliases)
               .FirstOrDefaultAsync(m => m.MemberId == memberId && m.CommunityId == communityId, cancellationToken);

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
