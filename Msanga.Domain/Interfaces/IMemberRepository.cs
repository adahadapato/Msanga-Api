using Msanga.Domain.Entities;

namespace Msanga.Domain.Interfaces;

/// <summary>Loads and adds <see cref="Member"/>s. Never saves: the caller saves once (<see cref="IUnitOfWork"/>).</summary>
public interface IMemberRepository
{
    /// <summary>Marks a new member to be inserted on the next save.</summary>
    /// <param name="member">The new member.</param>
    void Add(Member member);

    /// <summary>A member by Id, tracked (so changes are saved).</summary>
    /// <param name="id">The member's Id (= their account's Id).</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    /// <returns>The member, or null.</returns>
    Task<Member?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
