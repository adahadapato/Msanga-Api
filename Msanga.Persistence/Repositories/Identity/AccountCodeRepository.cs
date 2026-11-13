using Microsoft.EntityFrameworkCore;
using Msanga.Domain.Entities;
using Msanga.Domain.Enums;
using Msanga.Domain.Interfaces.Identity;

namespace Msanga.Persistence.Repositories.Identity;

/// <summary>EF Core implementation of <see cref="IAccountCodeRepository"/>.</summary>
internal sealed class AccountCodeRepository(MsangaDbContext context) : IAccountCodeRepository
{
    /// <inheritdoc />
    public void Add(AccountCode code) => context.AccountCodes.Add(code);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountCode>> GetUsableForUserAsync(Guid userId, AccountCodePurpose purpose,
                                                                        DateTime utcNow,
                                                                        CancellationToken cancellationToken = default) =>
        await context.AccountCodes
                     .Where(c => c.UserId == userId && c.Purpose == purpose && c.UsedAtUtc == null
                                 && c.ExpiresAtUtc > utcNow && c.FailedAttempts < AccountCode.MaxFailedAttempts)
                     .OrderByDescending(c => c.CreatedAtUtc)
                     .ToListAsync(cancellationToken);
}
