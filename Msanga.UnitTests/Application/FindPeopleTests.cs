using Msanga.Application.Features.People.Queries;
using Msanga.Domain.Entities;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Shared.Errors;
using Msanga.Domain.ValueObjects;

namespace Msanga.UnitTests.Application;

/// <summary>
/// Find People's visibility rule (decided 14 Nov 2026): you only search schools where you're confirmed; someone you
/// don't share a confirmed school with is "not found". A pretend repository stands in for the database.
/// </summary>
public sealed class FindPeopleTests
{
    private static readonly Guid Me = Guid.CreateVersion7();
    private static readonly Guid Fgcie = Guid.CreateVersion7();

    private sealed class Signed(Guid? id) : ICurrentUser
    {
        public Guid? UserId => id;
        public bool IsAuthenticated => id is not null;
        public bool IsInRole(string role) => false;
    }

    /// <summary>Only what Find People uses; it remembers what it was asked.</summary>
    private sealed class PretendMemberships(params Guid[] myConfirmedSchools) : ICommunityMembershipRepository
    {
        public (PeopleFilter Filter, IReadOnlyCollection<Guid> Schools, Guid Exclude, int Max)? Asked { get; private set; }

        public Task<IReadOnlyList<Guid>> ConfirmedSchoolIdsAsync(Guid memberId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(memberId == Me ? myConfirmedSchools : []);

        public Task<IReadOnlyList<CommunityMembership>> FindPeopleAsync(PeopleFilter filter, IReadOnlyCollection<Guid> schoolIds,
                                                                        Guid excludeMemberId, int maxPeople, CancellationToken ct = default)
        {
            Asked = (filter, schoolIds, excludeMemberId, maxPeople);
            return Task.FromResult<IReadOnlyList<CommunityMembership>>([]);
        }

        public Task<IReadOnlyList<CommunityMembership>> ConfirmedAtAsync(Guid memberId, IReadOnlyCollection<Guid> schoolIds,
                                                                         CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CommunityMembership>>([]);

        public void Add(CommunityMembership membership) => throw new NotSupportedException();
        public void Remove(CommunityMembership membership) => throw new NotSupportedException();
        public Task<CommunityMembership?> GetAsync(Guid memberId, Guid communityId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CountConfirmedAsync(Guid communityId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CommunityMembership>> ListWaitingAsync(Guid communityId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CommunityMembership>> ListConfirmedInSetAsync(Guid communityId, int setYear, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<CommunityMembership>> ListForMemberAsync(Guid memberId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class NoVouches : IVouchRepository
    {
        public void Add(Vouch vouch) => throw new NotSupportedException();
        public Task<bool> ExistsAsync(Guid c, Guid a, Guid b, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> CountForAsync(Guid c, Guid b, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> VoucherNamesAsync(Guid c, Guid b, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);
        public Task<IReadOnlySet<Guid>> VoucheesOfAsync(Guid c, Guid a, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private static FindPeopleQuery Search(string? text = null, Guid? school = null, string? country = null) =>
        new(text, school, SetYear: null, House: null, country);

    [Fact]
    public async Task Someone_not_confirmed_anywhere_is_told_to_get_confirmed_first()
    {
        var result = await new FindPeopleQueryHandler(new PretendMemberships(), new Signed(Me))
            .Handle(Search("nse"), CancellationToken.None);

        Assert.Equal(PeopleErrors.ConfirmFirst, result.Error);
    }

    [Fact]
    public async Task Searches_only_my_confirmed_schools_never_finds_me_and_tidies_the_words()
    {
        var memberships = new PretendMemberships(Fgcie);

        var result = await new FindPeopleQueryHandler(memberships, new Signed(Me))
            .Handle(Search("  nse   udo ", country: "gb"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var asked = Assert.NotNull(memberships.Asked);
        Guid[] onlyMine = [Fgcie];
        string[] words = ["nse", "udo"];
        Assert.Equal(onlyMine, asked.Schools);
        Assert.Equal(Me, asked.Exclude);
        Assert.Equal(FindPeopleQuery.MaxPeople, asked.Max);
        Assert.Equal(words, asked.Filter.NameWords);
        Assert.Equal("GB", asked.Filter.CountryCode);
    }

    [Fact]
    public async Task A_school_that_is_not_mine_finds_nobody_without_asking_the_database()
    {
        var memberships = new PretendMemberships(Fgcie);

        var result = await new FindPeopleQueryHandler(memberships, new Signed(Me))
            .Handle(Search(school: Guid.CreateVersion7()), CancellationToken.None);

        Assert.Empty(result.Value);
        Assert.Null(memberships.Asked);
    }

    [Fact]
    public async Task A_person_I_share_no_confirmed_school_with_is_not_found()
    {
        var result = await new GetPersonQueryHandler(new PretendMemberships(Fgcie), new NoVouches(), new Signed(Me))
            .Handle(new GetPersonQuery(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(PeopleErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task Before_I_am_confirmed_every_person_is_not_found()
    {
        var result = await new GetPersonQueryHandler(new PretendMemberships(), new NoVouches(), new Signed(Me))
            .Handle(new GetPersonQuery(Guid.CreateVersion7()), CancellationToken.None);

        Assert.Equal(PeopleErrors.NotFound, result.Error);
    }
}
