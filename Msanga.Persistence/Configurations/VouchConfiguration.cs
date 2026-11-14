using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Msanga.Domain.Entities;

namespace Msanga.Persistence.Configurations;

/// <summary>
/// Table <c>Vouches</c>: "I know them". One per voucher, person and school. Kept when the voucher's account goes
/// (no cascade from the voucher: the record of who vouched matters for tracing fakes); deleted with the person vouched
/// for, or with the school.
/// </summary>
public sealed class VouchConfiguration : EntityConfiguration<Vouch>
{
    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Vouch> builder)
    {
        builder.ToTable("Vouches", table =>
            table.HasCheckConstraint("CK_Vouches_NotYourself", "[VoucherId] <> [VoucheeId]"));

        builder.HasOne<Community>().WithMany().HasForeignKey(v => v.CommunityId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Member>().WithMany().HasForeignKey(v => v.VoucheeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Member>().WithMany().HasForeignKey(v => v.VoucherId).OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(v => new { v.CommunityId, v.VoucherId, v.VoucheeId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(v => new { v.CommunityId, v.VoucheeId });
    }
}
