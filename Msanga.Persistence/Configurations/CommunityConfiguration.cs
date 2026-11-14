using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Msanga.Domain.Entities;

namespace Msanga.Persistence.Configurations;

/// <summary>Table <c>Communities</c>: one row per school. Its other names are in <c>CommunityAliases</c>.</summary>
public sealed class CommunityConfiguration : EntityConfiguration<Community>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Community> builder)
    {
        builder.ToTable("Communities");

        builder.Property(c => c.Name).IsRequired().HasMaxLength(Community.NameMaxLength);
        builder.Property(c => c.Kind).IsRequired();
        builder.Property(c => c.Level).IsRequired();
        builder.Property(c => c.Town).IsRequired().HasMaxLength(Community.PlaceMaxLength);
        builder.Property(c => c.State).HasMaxLength(Community.PlaceMaxLength);
        builder.Property(c => c.CountryCode).IsRequired().HasMaxLength(Community.CountryCodeLength).IsFixedLength()
               .IsUnicode(false);
        builder.Property(c => c.Status).IsRequired();
        builder.Property(c => c.SearchKey).IsRequired().HasMaxLength(Community.SearchKeyMaxLength);

        // Who added it: kept if the member's account goes (admins still see it was a member), not a foreign key
        builder.Property(c => c.AddedByMemberId);

        // Other names: saved and deleted with their school; read through the list's field
        builder.HasMany(c => c.Aliases)
               .WithOne()
               .HasForeignKey(a => a.CommunityId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Aliases).UsePropertyAccessMode(PropertyAccessMode.Field);

        // "Did you mean…?" (M2b) looks schools up by key; lists filter by country
        builder.HasIndex(c => c.SearchKey);
        builder.HasIndex(c => c.CountryCode);
    }
}

/// <summary>Table <c>CommunityAliases</c>: other names for a school ("FGCIE"), each with its search key.</summary>
public sealed class CommunityAliasConfiguration : EntityConfiguration<CommunityAlias>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<CommunityAlias> builder)
    {
        builder.ToTable("CommunityAliases");

        builder.Property(a => a.Name).IsRequired().HasMaxLength(Community.NameMaxLength);
        builder.Property(a => a.SearchKey).IsRequired().HasMaxLength(Community.SearchKeyMaxLength);

        // A school can't have the same other name twice
        builder.HasIndex(a => new { a.CommunityId, a.SearchKey }).IsUnique();
        builder.HasIndex(a => a.SearchKey);
    }
}
