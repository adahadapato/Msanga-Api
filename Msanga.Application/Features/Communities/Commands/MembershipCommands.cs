using MediatR;
using Msanga.Application.Features.Communities.Dtos;
using Msanga.Domain.Entities;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Shared.Errors;
using Msanga.Domain.Shared.Results;

namespace Msanga.Application.Features.Communities.Commands;

/// <summary>The years at a school, as the form sends them (joining, or changing them).</summary>
public interface ISchoolYears
{
    /// <summary>The year they started there.</summary>
    int FromYear { get; }

    /// <summary>The year they left (or finished).</summary>
    int ToYear { get; }
}

/// <summary>"Were you a student here?" Yes: the signed-in member joins the school, status Joined.</summary>
/// <param name="CommunityId">The school (from the address).</param>
/// <param name="FromYear">The year they started.</param>
/// <param name="ToYear">The year they left.</param>
/// <param name="Finished">True if they finished there.</param>
public sealed record JoinCommunityCommand(Guid CommunityId, int FromYear, int ToYear, bool Finished)
    : IRequest<Result<MembershipDto>>, ISchoolYears;

/// <summary>Handles <see cref="JoinCommunityCommand"/>: the school must exist; joining twice is refused.</summary>
public sealed class JoinCommunityCommandHandler(
    ICommunityRepository communities,
    ICommunityMembershipRepository memberships,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<JoinCommunityCommand, Result<MembershipDto>>
{
    /// <inheritdoc />
    public async Task<Result<MembershipDto>> Handle(JoinCommunityCommand c, CancellationToken ct)
    {
        var memberId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var community = await communities.GetByIdAsync(c.CommunityId, ct);
        if (community is null) return CommunityErrors.NotFound;
        if (await memberships.GetAsync(memberId, c.CommunityId, ct) is not null) return CommunityErrors.AlreadyMember;

        var membership = CommunityMembership.Join(memberId, c.CommunityId, c.FromYear, c.ToYear, c.Finished,
                                                  clock.GetUtcNow().Year);
        memberships.Add(membership);
        await unitOfWork.SaveChangesAsync(ct);

        return MembershipDto.From(membership, community);
    }
}

/// <summary>Corrects your years at a school you've joined.</summary>
/// <param name="CommunityId">The school (from the address).</param>
/// <param name="FromYear">The year you started.</param>
/// <param name="ToYear">The year you left.</param>
/// <param name="Finished">True if you finished there.</param>
public sealed record ChangeMySchoolYearsCommand(Guid CommunityId, int FromYear, int ToYear, bool Finished)
    : IRequest<Result<MembershipDto>>, ISchoolYears;

/// <summary>Handles <see cref="ChangeMySchoolYearsCommand"/>.</summary>
public sealed class ChangeMySchoolYearsCommandHandler(
    ICommunityMembershipRepository memberships,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<ChangeMySchoolYearsCommand, Result<MembershipDto>>
{
    /// <inheritdoc />
    public async Task<Result<MembershipDto>> Handle(ChangeMySchoolYearsCommand c, CancellationToken ct)
    {
        var memberId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var membership = await memberships.GetAsync(memberId, c.CommunityId, ct);
        if (membership is null) return CommunityErrors.NotAMember;

        membership.ChangeYears(c.FromYear, c.ToYear, c.Finished, clock.GetUtcNow().Year);
        await unitOfWork.SaveChangesAsync(ct);

        return MembershipDto.From(membership, membership.Community);
    }
}

/// <summary>Leave a school: your membership is deleted (joining again starts afresh).</summary>
/// <param name="CommunityId">The school (from the address).</param>
public sealed record LeaveCommunityCommand(Guid CommunityId) : IRequest<Result>;

/// <summary>Handles <see cref="LeaveCommunityCommand"/>.</summary>
public sealed class LeaveCommunityCommandHandler(
    ICommunityMembershipRepository memberships,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<LeaveCommunityCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(LeaveCommunityCommand c, CancellationToken ct)
    {
        var memberId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var membership = await memberships.GetAsync(memberId, c.CommunityId, ct);
        if (membership is null) return CommunityErrors.NotAMember;

        memberships.Remove(membership);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
