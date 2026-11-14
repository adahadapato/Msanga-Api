using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Msanga.Domain.Entities;
using Msanga.Domain.Interfaces;
using Msanga.Persistence.Configurations.Identity;
using Msanga.Persistence.Converters;

namespace Msanga.Persistence;

/// <summary>
/// The one database context for Msanga: Identity's tables (users, roles) AND every Domain table.
/// Also the <see cref="IUnitOfWork"/>: repositories add and change entities, and one
/// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> saves them all in one transaction.
/// </summary>
/// <remarks>
/// <para><b>One context, one scope:</b> registered per request, so Identity's UserManager and our repositories share
/// the same instance.</para>
/// <para><b>Mappings</b> come from the <c>IEntityTypeConfiguration</c> classes in <c>Configurations/</c>, all loaded
/// with <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>; nothing is configured inline here.</para>
/// <para><b>Audit columns</b> (CreatedAt / UpdatedAt) are filled by the audit interceptor, registered in
/// DependencyInjection.</para>
/// </remarks>
public sealed class MsangaDbContext(DbContextOptions<MsangaDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IUnitOfWork
{
    // ---------- People ----------

    /// <summary>Members: one per account, same Id.</summary>
    public DbSet<Member> Members => Set<Member>();

    // ---------- Communities (schools) ----------

    /// <summary>Schools: official name, level, place, status, search key.</summary>
    public DbSet<Community> Communities => Set<Community>();

    /// <summary>Other names for schools ("FGCIE"), each with its search key.</summary>
    public DbSet<CommunityAlias> CommunityAliases => Set<CommunityAlias>();

    /// <summary>Who was a student at which school, and when (school first, then the year).</summary>
    public DbSet<CommunityMembership> CommunityMemberships => Set<CommunityMembership>();

    // ---------- Sign-in ----------

    /// <summary>Hashed refresh tokens, managed by the Identity services.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Hashed 6-digit confirm-email and reset-password codes, managed by the Identity services.</summary>
    public DbSet<AccountCode> AccountCodes => Set<AccountCode>();

    /// <summary>Identity's tables first, then every configuration in this assembly.</summary>
    /// <param name="builder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);   // Identity's tables (must come first)
        builder.ApplyConfigurationsFromAssembly(typeof(MsangaDbContext).Assembly);
    }

    /// <summary>Defaults for every column of a type, unless a configuration says otherwise.</summary>
    /// <param name="configurationBuilder">The conventions builder.</param>
    /// <remarks><b>Dates:</b> every DateTime is UTC and read back marked as UTC (so JSON writes the "Z").</remarks>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Starts a database transaction on this context. Everything saved through this context (UserManager,
    /// repositories) until <see cref="ITransaction.CommitAsync"/> is committed together, or rolled back together.
    /// </remarks>
    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await Database.BeginTransactionAsync(cancellationToken));
}
