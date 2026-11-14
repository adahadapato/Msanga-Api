using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Msanga.Domain.Entities;
using Msanga.Persistence.Configurations.Identity;

namespace Msanga.Persistence.Configurations;

/// <summary>
/// Table <c>Members</c>: one row per person, with the same Id as their sign-in account (AspNetUsers). Deleting the
/// account deletes the member.
/// </summary>
public sealed class MemberConfiguration : EntityConfiguration<Member>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");

        builder.Property(m => m.FirstName).IsRequired().HasMaxLength(Member.NameMaxLength);
        builder.Property(m => m.LastName).IsRequired().HasMaxLength(Member.NameMaxLength);
        builder.Property(m => m.SchoolSurname).HasMaxLength(Member.NameMaxLength);
        builder.Property(m => m.Bio).IsRequired().HasMaxLength(Member.BioMaxLength).HasDefaultValue(string.Empty);
        builder.Property(m => m.PhotoUrl).HasMaxLength(Member.PhotoUrlMaxLength);
        builder.Property(m => m.LivesInCity).HasMaxLength(Member.CityMaxLength);
        builder.Property(m => m.LivesInCountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false);
        builder.HasIndex(m => m.LivesInCountryCode);   // Find People: filter by country
        builder.Ignore(m => m.DisplayName);                                     // worked out from the names

        // Same Id as the account: one account, one member
        builder.HasOne<ApplicationUser>()
               .WithOne()
               .HasForeignKey<Member>(m => m.Id)
               .OnDelete(DeleteBehavior.Cascade);

        // Finding classmates by name (M2–M3) searches these
        builder.HasIndex(m => new { m.LastName, m.FirstName });
        builder.HasIndex(m => m.SchoolSurname);
    }
}
