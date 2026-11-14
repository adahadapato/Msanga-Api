using Msanga.Domain.Entities;

namespace Msanga.Domain.Interfaces;

/// <summary>Finds and adds <see cref="Community"/>s (schools). Never saves: the caller saves once
/// (<see cref="IUnitOfWork"/>).</summary>
public interface ICommunityRepository
{
    /// <summary>Marks a new school (and its other names) to be inserted on the next save.</summary>
    /// <param name="community">The new school.</param>
    void Add(Community community);

    /// <summary>A school by Id, with its other names.</summary>
    /// <param name="id">The school's Id.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The school, or null.</returns>
    Task<Community?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schools where every search word is the start of a word in the school's key or one of its other names' keys.
    /// Best first: a key that starts with the whole search, then Listed before New, then A–Z.
    /// </summary>
    /// <param name="words">The search's words (<c>SearchKey.Words</c>), at least one.</param>
    /// <param name="max">The most schools to return.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The schools, with their other names, read-only.</returns>
    Task<IReadOnlyList<Community>> SearchAsync(IReadOnlyList<string> words, int max,
                                               CancellationToken cancellationToken = default);
}
