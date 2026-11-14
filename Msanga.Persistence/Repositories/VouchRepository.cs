using Microsoft.EntityFrameworkCore;
using Msanga.Domain.Entities;
using Msanga.Domain.Interfaces;

namespace Msanga.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IVouchRepository"/>.</summary>
internal sealed class VouchRepository(MsangaDbContext context) : IVouchRepository
{
    /// <inheritdoc />
    public void Add(Vouch vouch) => context.Vouches.Add(vouch);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid communityId, Guid voucherId, Guid voucheeId,
                                  CancellationToken cancellationToken = default) =>
        context.Vouches.AnyAsync(v => v.CommunityId == communityId && v.VoucherId == voucherId
                                   && v.VoucheeId == voucheeId, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountForAsync(Guid communityId, Guid voucheeId, CancellationToken cancellationToken = default) =>
        context.Vouches.CountAsync(v => v.CommunityId == communityId && v.VoucheeId == voucheeId, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> VoucherNamesAsync(Guid communityId, Guid voucheeId,
                                                              CancellationToken cancellationToken = default)
    {
        var vouchers = await (from v in context.Vouches.AsNoTracking()
                              join m in context.Members.AsNoTracking() on v.VoucherId equals m.Id
                              where v.CommunityId == communityId && v.VoucheeId == voucheeId
                              orderby v.CreatedAt
                              select m)
                             .ToListAsync(cancellationToken);
        return vouchers.Select(m => m.DisplayName).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<Guid>> VoucheesOfAsync(Guid communityId, Guid voucherId,
                                                         CancellationToken cancellationToken = default) =>
        (await context.Vouches.AsNoTracking()
                      .Where(v => v.CommunityId == communityId && v.VoucherId == voucherId)
                      .Select(v => v.VoucheeId)
                      .ToListAsync(cancellationToken))
        .ToHashSet();
}
