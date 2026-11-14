using Msanga.Domain.Entities;
using Msanga.Domain.Enums;

namespace Msanga.Domain.Rules;

/// <summary>
/// The vouching rules (decided 14 Nov 2026), in one place.
/// </summary>
/// <remarks>
/// <para><b>Confirmed</b> = <see cref="VouchesToConfirm"/> people said "I know them".</para>
/// <para><b>Who may vouch:</b> a Confirmed member of the same school, from any year; or, in a school's early days (fewer
/// than <see cref="EarlyDaysConfirmedMembers"/> confirmed members), a Joined member too, so a new school can start
/// without an admin. An admin can also confirm someone directly (the pilot's first people).</para>
/// <para>A vouch counts when it's made; it isn't re-checked later (the early days ending doesn't undo vouches).</para>
/// </remarks>
public static class Vouching
{
    /// <summary>"I know them" from this many people confirms someone.</summary>
    public const int VouchesToConfirm = 2;

    /// <summary>Until a school has this many confirmed members, its Joined members may vouch too.</summary>
    public const int EarlyDaysConfirmedMembers = 3;

    /// <summary>Most vouches per member per day (rate limit), so nobody can confirm a crowd of fakes.</summary>
    public const int MaxVouchesPerDay = 20;

    /// <summary>Is this school still in its early days?</summary>
    /// <param name="confirmedMembers">How many confirmed members it has.</param>
    /// <returns>True if Joined members may vouch.</returns>
    public static bool InEarlyDays(int confirmedMembers) => confirmedMembers < EarlyDaysConfirmedMembers;

    /// <summary>May this member vouch at their school?</summary>
    /// <param name="voucher">Their membership of the school.</param>
    /// <param name="confirmedMembers">How many confirmed members the school has.</param>
    /// <returns>True if Confirmed, or Joined in the school's early days.</returns>
    public static bool MayVouch(CommunityMembership voucher, int confirmedMembers) =>
        voucher.Status == CommunityMembershipStatus.Confirmed || InEarlyDays(confirmedMembers);

    /// <summary>Do this many vouches confirm someone?</summary>
    /// <param name="vouches">Vouches they have, the new one included.</param>
    /// <returns>True at <see cref="VouchesToConfirm"/> or more.</returns>
    public static bool Confirms(int vouches) => vouches >= VouchesToConfirm;
}
