using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Msanga.Domain.Entities;

namespace Msanga.Persistence.Configurations.Identity;

/// <summary>
/// Table <c>AccountLinks</c>: hashed confirm-email and reset-password links, linked to their user. Deleting a user
/// deletes their links.
/// </summary>
/// <remarks>Like RefreshTokens: no audit / soft-delete columns. Links are used or expire, then can be cleaned up.</remarks>
public sealed class AccountLinkConfiguration : IEntityTypeConfiguration<AccountLink>
{
    /// <summary>Maps the table.</summary>
    /// <param name="builder">EF Core's builder for this entity.</param>
    public void Configure(EntityTypeBuilder<AccountLink> builder)
    {
        builder.ToTable("AccountLinks");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Purpose).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(l => l.TokenHash).IsRequired().HasMaxLength(AccountLink.TokenHashLength).IsFixedLength();
        builder.HasIndex(l => l.TokenHash).IsUnique();                         // look-up when the link is opened

        builder.Property(l => l.CreatedAtUtc).IsRequired();
        builder.Property(l => l.ExpiresAtUtc).IsRequired();
        builder.Property(l => l.UsedAtUtc);

        builder.HasOne<ApplicationUser>()
               .WithMany()
               .HasForeignKey(l => l.UserId)
               .IsRequired()
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(l => new { l.UserId, l.Purpose, l.UsedAtUtc });     // "retire this user's older links"
    }
}
