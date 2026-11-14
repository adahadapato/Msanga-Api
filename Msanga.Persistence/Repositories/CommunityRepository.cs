using Microsoft.EntityFrameworkCore;
using Msanga.Domain.Entities;
using Msanga.Domain.Interfaces;

namespace Msanga.Persistence.Repositories;

/// <summary>EF Core implementation of <see cref="ICommunityRepository"/>.</summary>
/// <remarks>
/// <para><b>How search matches:</b> keys are words separated by single spaces, so "a word starts with ikot" is
/// <c>' ' + key LIKE '% ikot%'</c>. Search words are only letters and digits (<c>SearchKey</c> removes everything
/// else), so they can't contain LIKE's special characters.</para>
/// <para>A LIKE starting with % can't use an index: fine for thousands of schools. If search ever gets slow, the
/// next step is SQL Server full-text search; the endpoint stays the same.</para>
/// </remarks>
internal sealed class CommunityRepository(MsangaDbContext context) : ICommunityRepository
{
    /// <inheritdoc />
    public void Add(Community community) => context.Communities.Add(community);

    /// <inheritdoc />
    public Task<Community?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Communities.Include(c => c.Aliases).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task<Community?> FindBySearchKeyAsync(string searchKey, CancellationToken cancellationToken = default) =>
        context.Communities.AsNoTracking().Include(c => c.Aliases)
               .FirstOrDefaultAsync(c => c.SearchKey == searchKey, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Community>> SearchAsync(IReadOnlyList<string> words, int max,
                                                            CancellationToken cancellationToken = default)
    {
        var query = context.Communities.AsNoTracking();

        // Every word must start a word in the school's key or in one of its other names' keys
        foreach (var word in words)
        {
            var pattern = $"% {word}%";
            query = query.Where(c => EF.Functions.Like(" " + c.SearchKey, pattern)
                                  || c.Aliases.Any(a => EF.Functions.Like(" " + a.SearchKey, pattern)));
        }

        // Best first: a key starting with the whole search ("unilag" → UNILAG), then Listed, then A–Z
        var whole = string.Join(' ', words);
        return await query
            .OrderByDescending(c => c.SearchKey.StartsWith(whole) || c.Aliases.Any(a => a.SearchKey.StartsWith(whole)))
            .ThenBy(c => c.Status)
            .ThenBy(c => c.Name)
            .Take(max)
            .Include(c => c.Aliases)
            .ToListAsync(cancellationToken);
    }
}
