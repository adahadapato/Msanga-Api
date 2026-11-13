using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Msanga.Domain.Entities;

namespace Msanga.Persistence.Configurations.Identity;

/// <summary>
/// Table <c>AccountCodes</c>: hashed 6-digit confirm-email and reset-password codes, linked to their user. Deleting a
/// user deletes their codes.
/// </summary>
/// <remarks>Like RefreshTokens: no audit / soft-delete columns. Codes are used, retired or expire, then can be
/// cleaned up.</remarks>
public sealed class AccountCodeConfiguration : IEntityTypeConfiguration<AccountCode>
{
    /// <summary>Maps the table.</summary>
    /// <param name="builder">EF Core's builder for this entity.</param>
    public void Configure(EntityTypeBuilder<AccountCode> builder)
    {
        builder.ToTable("AccountCodes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Purpose).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.CodeHash).IsRequired().HasMaxLength(AccountCode.CodeHashLength).IsFixedLength();
        builder.Property(c => c.CreatedAtUtc).IsRequired();
        builder.Property(c => c.ExpiresAtUtc).IsRequired();
        builder.Property(c => c.UsedAtUtc);
        builder.Property(c => c.FailedAttempts).IsRequired().HasDefaultValue(0);

        builder.HasOne<ApplicationUser>()
               .WithMany()
               .HasForeignKey(c => c.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);

        // "This account's working codes of this kind": checking a typed code, and retiring older ones
        builder.HasIndex(c => new { c.UserId, c.Purpose, c.UsedAtUtc });
    }
}
