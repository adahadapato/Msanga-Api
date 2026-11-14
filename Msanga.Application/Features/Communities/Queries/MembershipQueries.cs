using MediatR;
using Msanga.Application.Features.Communities.Dtos;
using Msanga.Domain.Entities;
using Msanga.Domain.Enums;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Rules;
using Msanga.Domain.Shared.Errors;
using Msanga.Domain.Shared.Results;

namespace Msanga.Application.Features.Communities.Queries;

/// <summary>My Schools: the signed-in member's schools and years, most recent first.</summary>
public sealed record GetMySchoolsQuery : IRequest<Result<IReadOnlyList<MembershipDto>>>;

/// <summary>Handles <see cref="GetMySchoolsQuery"/>.</summary>
public sealed class GetMySchoolsQueryHandler(ICommunityMembershipRepository memberships, ICurrentUser currentUser)
    : IRequestHandler<GetMySchoolsQuery, Result<IReadOnlyList<MembershipDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MembershipDto>>> Handle(GetMySchoolsQuery query, CancellationToken ct)
    {
        var memberId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var mine = await memberships.ListForMemberAsync(memberId, ct);
        return Result.Success<IReadOnlyList<MembershipDto>>(mine.Select(m => MembershipDto.From(m, m.Community)).ToList());
    }
}

/// <summary>Have I joined this school? (The school's page asks, to show "You were here…" or "Were you a student
/// here?".)</summary>
/// <param name="CommunityId">The school.</param>
public sealed record GetMyMembershipQuery(Guid CommunityId) : IRequest<Result<MembershipDto>>;

/// <summary>Handles <see cref="GetMyMembershipQuery"/>: 404 <c>Community.NotAMember</c> if not. Includes who vouched
/// for me and whether I may vouch here (the school's page uses both).</summary>
public sealed class GetMyMembershipQueryHandler(
    ICommunityMembershipRepository memberships,
    IVouchRepository vouches,
    ICurrentUser currentUser) : IRequestHandler<GetMyMembershipQuery, Result<MembershipDto>>
{
    /// <inheritdoc />
    public async Task<Result<MembershipDto>> Handle(GetMyMembershipQuery query, CancellationToken ct)
    {
        var memberId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var membership = await memberships.GetAsync(memberId, query.CommunityId, ct);
        if (membership is null) return CommunityErrors.NotAMember;

        var vouchedBy = await vouches.VoucherNamesAsync(query.CommunityId, memberId, ct);
        var canVouch = Vouching.MayVouch(membership, await memberships.CountConfirmedAsync(query.CommunityId, ct));
        return MembershipDto.From(membership, membership.Community, vouchedBy, canVouch);
    }
}

/// <summary>People waiting to be confirmed at a school (not me): for "I know them".</summary>
/// <param name="CommunityId">The school.</param>
public sealed record GetWaitingQuery(Guid CommunityId) : IRequest<Result<IReadOnlyList<PersonDto>>>;

/// <summary>Handles <see cref="GetWaitingQuery"/>: only for those who may vouch (confirmed, or the school's early
/// days); 403 <c>Community.MembersOnly</c> / <c>Community.NotConfirmedYet</c> otherwise.</summary>
public sealed class GetWaitingQueryHandler(
    ICommunityMembershipRepository memberships,
    IVouchRepository vouches,
    ICurrentUser currentUser) : IRequestHandler<GetWaitingQuery, Result<IReadOnlyList<PersonDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PersonDto>>> Handle(GetWaitingQuery query, CancellationToken ct)
    {
        var memberId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var mine = await memberships.GetAsync(memberId, query.CommunityId, ct);
        if (mine is null) return CommunityErrors.MembersOnly;
        if (!Vouching.MayVouch(mine, await memberships.CountConfirmedAsync(query.CommunityId, ct)))
            return CommunityErrors.NotConfirmedYet;

        var iVouched = await vouches.VoucheesOfAsync(query.CommunityId, memberId, ct);
        var waiting = await memberships.ListWaitingAsync(query.CommunityId, ct);
        return Result.Success<IReadOnlyList<PersonDto>>(
            waiting.Where(m => m.MemberId != memberId)
                   .Select(m => PersonDto.From(m, iVouched.Contains(m.MemberId)))
                   .ToList());
    }
}

/// <summary>A set's page ("1998 Set at FGC Ikot Ekpene"). Only for people who joined the school (school first); who's
/// in it only for those confirmed there.</summary>
/// <param name="CommunityId">The school.</param>
/// <param name="Year">The set's year.</param>
public sealed record GetSetQuery(Guid CommunityId, int Year) : IRequest<Result<SetDto>>
{
    /// <summary>The latest set year: a set can finish a few years from now (someone who left early).</summary>
    public const int YearsAhead = CommunityMembership.SetWindowYears;
}

/// <summary>Handles <see cref="GetSetQuery"/>: 404 if there's no such school, 403 <c>Community.MembersOnly</c> if you
/// haven't joined it.</summary>
public sealed class GetSetQueryHandler(
    ICommunityRepository communities,
    ICommunityMembershipRepository memberships,
    ICurrentUser currentUser) : IRequestHandler<GetSetQuery, Result<SetDto>>
{
    /// <inheritdoc />
    public async Task<Result<SetDto>> Handle(GetSetQuery query, CancellationToken ct)
    {
        var memberId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var school = await communities.GetByIdAsync(query.CommunityId, ct);
        if (school is null) return CommunityErrors.NotFound;

        var mine = await memberships.GetAsync(memberId, query.CommunityId, ct);
        if (mine is null) return CommunityErrors.MembersOnly;

        // Who's here: only confirmed members of the school see it
        var canSee = mine.Status == CommunityMembershipStatus.Confirmed;
        var members = canSee
            ? (await memberships.ListConfirmedInSetAsync(query.CommunityId, query.Year, ct)).Select(m => PersonDto.From(m)).ToList()
            : [];

        return new SetDto(CommunityDto.From(school), query.Year, Community.SetName(query.Year), mine.SetYear == query.Year,
                          canSee, members);
    }
}
