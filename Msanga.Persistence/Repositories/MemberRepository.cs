using Microsoft.EntityFrameworkCore;
using Msanga.Domain.Entities;
using Msanga.Domain.Interfaces;

namespace Msanga.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="IMemberRepository"/>.</summary>
internal sealed class MemberRepository(MsangaDbContext context) : IMemberRepository
{
    /// <inheritdoc />
    public void Add(Member member) => context.Members.Add(member);

    /// <inheritdoc />
    public Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Members.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
}
