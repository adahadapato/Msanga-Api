using MediatR;
using Msanga.Application.Features.People.Dtos;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Shared.Errors;
using Msanga.Domain.Shared.Results;
using Msanga.Domain.ValueObjects;

namespace Msanga.Application.Features.People.Queries;

/// <summary>
/// Find People (decided 14 Nov 2026): people confirmed at the schools where YOU are confirmed, by name (the surname at
/// school too) and filters. Nobody else is ever found; you're never in your own results.
/// </summary>
/// <param name="Search">Name words; blank = everyone (then filters narrow it).</param>
/// <param name="SchoolId">Only this school (if it isn't one of yours: nobody).</param>
/// <param name="SetYear">Only this set.</param>
/// <param name="House">House containing this.</param>
/// <param name="Country">Lives in this country now (two letters).</param>
public sealed record FindPeopleQuery(string? Search, Guid? SchoolId, int? SetYear, string? House, string? Country)
    : IRequest<Result<IReadOnlyList<PersonCardDto>>>
{
    /// <summary>The most people in one answer.</summary>
    public const int MaxPeople = 50;

    /// <summary>Longest search text.</summary>
    public const int SearchMaxLength = 100;
}

/// <summary>Handles <see cref="FindPeopleQuery"/>: 403 <c>People.ConfirmFirst</c> if you aren't confirmed anywhere.</summary>
public sealed class FindPeopleQueryHandler(ICommunityMembershipRepository memberships, ICurrentUser currentUser)
    : IRequestHandler<FindPeopleQuery, Result<IReadOnlyList<PersonCardDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PersonCardDto>>> Handle(FindPeopleQuery q, CancellationToken ct)
    {
        var me = currentUser.UserId ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var mySchools = await memberships.ConfirmedSchoolIdsAsync(me, ct);
        if (mySchools.Count == 0) return PeopleErrors.ConfirmFirst;

        // A school that isn't one of yours finds nobody (no hint that it has members)
        if (q.SchoolId is { } school && !mySchools.Contains(school))
            return Result.Success<IReadOnlyList<PersonCardDto>>([]);

        var words = (q.Search ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var country = string.IsNullOrWhiteSpace(q.Country) ? null : q.Country.Trim().ToUpperInvariant();
        var filter = new PeopleFilter(words, q.SchoolId, q.SetYear, q.House, country);

        var found = await memberships.FindPeopleAsync(filter, mySchools, me, FindPeopleQuery.MaxPeople, ct);
        var people = found
            .GroupBy(m => m.MemberId)
            .Select(g =>
            {
                var member = g.First().Member;
                return new PersonCardDto(member.Id, $"{member.FirstName} {member.LastName}", member.SchoolSurname,
                                         member.LivesInCity, member.LivesInCountryCode,
                                         g.OrderBy(m => m.Community.Name).Select(m => SharedSchoolDto.From(m)).ToList());
            })
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return Result.Success<IReadOnlyList<PersonCardDto>>(people);
    }
}

/// <summary>A person's page: only if you share a school where you're both confirmed.</summary>
/// <param name="MemberId">The person.</param>
public sealed record GetPersonQuery(Guid MemberId) : IRequest<Result<PersonPageDto>>;

/// <summary>Handles <see cref="GetPersonQuery"/>: 404 <c>People.NotFound</c> for anyone you don't share a confirmed
/// school with (the same answer as "no such person", so nobody learns who's hidden). Your own page works too.</summary>
public sealed class GetPersonQueryHandler(
    ICommunityMembershipRepository memberships,
    IVouchRepository vouches,
    ICurrentUser currentUser) : IRequestHandler<GetPersonQuery, Result<PersonPageDto>>
{
    /// <inheritdoc />
    public async Task<Result<PersonPageDto>> Handle(GetPersonQuery q, CancellationToken ct)
    {
        var me = currentUser.UserId ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var mySchools = await memberships.ConfirmedSchoolIdsAsync(me, ct);
        if (mySchools.Count == 0) return PeopleErrors.NotFound;

        var shared = await memberships.ConfirmedAtAsync(q.MemberId, mySchools, ct);
        if (shared.Count == 0) return PeopleErrors.NotFound;

        var schools = new List<SharedSchoolDto>();
        foreach (var m in shared)
            schools.Add(SharedSchoolDto.From(m, await vouches.VoucherNamesAsync(m.CommunityId, q.MemberId, ct)));

        var member = shared[0].Member;
        return new PersonPageDto(member.Id, $"{member.FirstName} {member.LastName}", member.SchoolSurname, member.Bio,
                                 member.LivesInCity, member.LivesInCountryCode, schools);
    }
}
