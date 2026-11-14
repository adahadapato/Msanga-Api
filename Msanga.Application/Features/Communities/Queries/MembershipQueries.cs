using MediatR;
using Msanga.Application.Features.Communities.Dtos;
using Msanga.Domain.Interfaces;
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

/// <summary>Handles <see cref="GetMyMembershipQuery"/>: 404 <c>Community.NotAMember</c> if not.</summary>
public sealed class GetMyMembershipQueryHandler(ICommunityMembershipRepository memberships, ICurrentUser currentUser)
    : IRequestHandler<GetMyMembershipQuery, Result<MembershipDto>>
{
    /// <inheritdoc />
    public async Task<Result<MembershipDto>> Handle(GetMyMembershipQuery query, CancellationToken ct)
    {
        var memberId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var membership = await memberships.GetAsync(memberId, query.CommunityId, ct);
        return membership is null ? CommunityErrors.NotAMember : MembershipDto.From(membership, membership.Community);
    }
}
