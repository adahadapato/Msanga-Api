namespace Msanga.Domain.Entities;

/// <summary>
/// "I know them" (table Vouches): one member vouching that another was a student at the same school. Kept for good
/// (who, whom, where, when), so a fake that slips through can be traced and undone (M7). One per voucher, person and
/// school.
/// </summary>
public sealed class Vouch : Entity<Guid>
{
    /// <summary>Required by EF Core.</summary>
    private Vouch() { }

    private Vouch(Guid communityId, Guid voucherId, Guid voucheeId) : base(Guid.CreateVersion7())
    {
        CommunityId = communityId;
        VoucherId = voucherId;
        VoucheeId = voucheeId;
    }

    /// <summary>A vouch. The rules (who may vouch, when it confirms) are in <see cref="Vouching"/>.</summary>
    /// <param name="communityId">The school.</param>
    /// <param name="voucherId">Who says "I know them".</param>
    /// <param name="voucheeId">About whom.</param>
    /// <returns>The vouch.</returns>
    /// <exception cref="ArgumentException">Vouching for yourself.</exception>
    public static Vouch Create(Guid communityId, Guid voucherId, Guid voucheeId) =>
        voucherId == voucheeId
            ? throw new ArgumentException("You can't vouch for yourself.", nameof(voucheeId))
            : new Vouch(communityId, voucherId, voucheeId);

    /// <summary>The school.</summary>
    public Guid CommunityId { get; private set; }

    /// <summary>Who said "I know them".</summary>
    public Guid VoucherId { get; private set; }

    /// <summary>The member they vouched for.</summary>
    public Guid VoucheeId { get; private set; }
}
