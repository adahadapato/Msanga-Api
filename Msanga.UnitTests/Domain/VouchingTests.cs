using Msanga.Application.Features.Communities;
using Msanga.Domain.Entities;
using Msanga.Domain.Enums;
using Msanga.Domain.Rules;

namespace Msanga.UnitTests.Domain;

/// <summary>"I know them": who may vouch, and when someone becomes Confirmed (decided 14 Nov 2026).</summary>
public sealed class VouchingTests
{
    private static readonly Guid School = Guid.CreateVersion7();

    private static CommunityMembership Joined() =>
        CommunityMembership.Join(Guid.CreateVersion7(), School, 1992, 1998, finished: true, currentYear: 2026);

    private static CommunityMembership Confirmed()
    {
        var membership = Joined();
        membership.Confirm(new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc));
        return membership;
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]     // two people said "I know them"
    [InlineData(3, true)]
    public void Two_vouches_confirm_someone(int vouches, bool confirms) =>
        Assert.Equal(confirms, Vouching.Confirms(vouches));

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(500)]
    public void A_confirmed_member_may_always_vouch(int confirmedAtSchool) =>
        Assert.True(Vouching.MayVouch(Confirmed(), confirmedAtSchool));

    [Theory]
    [InlineData(0, true)]     // a brand-new school: its first people vouch for each other
    [InlineData(2, true)]
    [InlineData(3, false)]    // 3 confirmed: the early days are over
    [InlineData(40, false)]
    public void A_joined_member_may_vouch_only_in_the_school_s_early_days(int confirmedAtSchool, bool mayVouch) =>
        Assert.Equal(mayVouch, Vouching.MayVouch(Joined(), confirmedAtSchool));

    [Fact]
    public void Nobody_vouches_for_themselves()
    {
        var me = Guid.CreateVersion7();
        Assert.Throws<ArgumentException>(() => Vouch.Create(School, me, me));
    }

    [Fact]
    public void A_vouch_remembers_who_whom_and_where()
    {
        var (voucher, vouchee) = (Guid.CreateVersion7(), Guid.CreateVersion7());
        var vouch = Vouch.Create(School, voucher, vouchee);
        Assert.Equal((School, voucher, vouchee), (vouch.CommunityId, vouch.VoucherId, vouch.VoucheeId));
    }

    [Fact]
    public void Confirming_sets_the_status_and_time_once()
    {
        var membership = Joined();
        var first = new DateTime(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

        membership.Confirm(first);
        membership.Confirm(first.AddDays(3));    // again: nothing changes

        Assert.Equal(CommunityMembershipStatus.Confirmed, membership.Status);
        Assert.Equal(first, membership.ConfirmedAt);
    }

    [Fact]
    public void The_confirmed_email_names_the_school_and_who_vouched_and_has_no_links()
    {
        var (subject, html, text) = ConfirmedEmail.Build("Hilary", "Federal Government College, Ikot Ekpene",
                                                         ["Nse U.", "Utibe E."]);

        Assert.Equal("You're confirmed at Federal Government College, Ikot Ekpene", subject);
        Assert.Contains("Confirmed by Nse U. and Utibe E.\n", text.Replace("\r\n", "\n"));   // one dot, not two
        Assert.DoesNotContain("E..", html + text);
        Assert.DoesNotContain("http", html + text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<a", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void An_admin_confirmation_says_Msanga_and_names_are_made_safe_for_HTML()
    {
        var (_, html, text) = ConfirmedEmail.Build("<b>Ada</b>", "King's College", []);
        Assert.Contains("Confirmed by Msanga.", text);
        Assert.Contains("&lt;b&gt;Ada&lt;/b&gt;", html);
    }
}
