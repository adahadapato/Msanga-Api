using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Msanga.Domain.Entities;

namespace Msanga.Persistence.Configurations;

/// <summary>
/// Table <c>CommunityMemberships</c>: who was a student at which school, and when. One row per member per school.
/// Deleted with the member, or with the school.
/// </summary>
public sealed class CommunityMembershipConfiguration : EntityConfiguration<CommunityMembership>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<CommunityMembership> builder)
    {
        builder.ToTable("CommunityMemberships", table =>
        {
            // The same rules as CommunityMembership.YearsProblem, as a last guard in the database
            table.HasCheckConstraint("CK_CommunityMemberships_Years",
                $"[FromYear] >= {CommunityMembership.EarliestYear} AND [ToYear] >= [FromYear] " +
                $"AND [ToYear] - [FromYear] < {CommunityMembership.MaxYearsAtOneSchool}");
        });

        builder.Property(m => m.FromYear).IsRequired();
        builder.Property(m => m.ToYear).IsRequired();
        builder.Property(m => m.Finished).IsRequired();
        builder.Property(m => m.Status).IsRequired();

        // Your set and details (M3b): all optional
        builder.Property(m => m.SetYear);
        builder.Property(m => m.House).HasMaxLength(CommunityMembership.DetailMaxLength);
        builder.Property(m => m.ClassArm).HasMaxLength(CommunityMembership.DetailMaxLength);
        builder.Property(m => m.Hostel).HasMaxLength(CommunityMembership.DetailMaxLength);
        builder.Property(m => m.Department).HasMaxLength(CommunityMembership.DetailMaxLength);

        builder.HasOne<Member>()
               .WithMany()
               .HasForeignKey(m => m.MemberId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Community)
               .WithMany()
               .HasForeignKey(m => m.CommunityId)
               .OnDelete(DeleteBehavior.Cascade);

        // One membership per member per school (rows hidden by soft delete don't count)
        builder.HasIndex(m => new { m.MemberId, m.CommunityId }).IsUnique().HasFilter("[IsDeleted] = 0");

        // A school's members by year and by set (M3d Find People, set pages)
        builder.HasIndex(m => new { m.CommunityId, m.ToYear });
        builder.HasIndex(m => new { m.CommunityId, m.SetYear });
    }
}
