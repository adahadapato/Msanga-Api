using MediatR;
using Msanga.Application.Features.Communities.Dtos;
using Msanga.Domain.Entities;
using Msanga.Domain.Enums;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Rules;
using Msanga.Domain.Shared.Errors;
using Msanga.Domain.Shared.Results;

namespace Msanga.Application.Features.Communities.Commands;

/// <summary>"I know them": the signed-in member vouches that someone was a student at this school.</summary>
/// <param name="CommunityId">The school.</param>
/// <param name="MemberId">The person waiting.</param>
public sealed record VouchCommand(Guid CommunityId, Guid MemberId) : IRequest<Result<VouchOutcomeDto>>;

/// <summary>Handles <see cref="VouchCommand"/> with the rules in <see cref="Vouching"/>.</summary>
/// <remarks>
/// In order: you joined the school (else 403 MembersOnly); not yourself (400); they joined it (404 PersonNotFound);
/// they're not confirmed yet (409 AlreadyConfirmed); you may vouch (confirmed, or the school's early days; else 403
/// NotConfirmedYet); not twice (409 AlreadyVouched). The second vouch confirms them, and they get an email.
/// </remarks>
public sealed class VouchCommandHandler(
    ICommunityMembershipRepository memberships,
    IVouchRepository vouches,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock,
    ConfirmedEmail confirmedEmail) : IRequestHandler<VouchCommand, Result<VouchOutcomeDto>>
{
    /// <inheritdoc />
    public async Task<Result<VouchOutcomeDto>> Handle(VouchCommand c, CancellationToken ct)
    {
        var voucherId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var mine = await memberships.GetAsync(voucherId, c.CommunityId, ct);
        if (mine is null) return CommunityErrors.MembersOnly;
        if (c.MemberId == voucherId) return CommunityErrors.CannotVouchForYourself;

        var theirs = await memberships.GetAsync(c.MemberId, c.CommunityId, ct);
        if (theirs is null) return CommunityErrors.PersonNotFound;
        if (theirs.Status == CommunityMembershipStatus.Confirmed) return CommunityErrors.AlreadyConfirmed;

        var confirmedMembers = await memberships.CountConfirmedAsync(c.CommunityId, ct);
        if (!Vouching.MayVouch(mine, confirmedMembers)) return CommunityErrors.NotConfirmedYet;
        if (await vouches.ExistsAsync(c.CommunityId, voucherId, c.MemberId, ct)) return CommunityErrors.AlreadyVouched;

        vouches.Add(Vouch.Create(c.CommunityId, voucherId, c.MemberId));
        var nowConfirmed = Vouching.Confirms(await vouches.CountForAsync(c.CommunityId, c.MemberId, ct) + 1);
        if (nowConfirmed) theirs.Confirm(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);

        if (nowConfirmed)
        {
            var by = await vouches.VoucherNamesAsync(c.CommunityId, c.MemberId, ct);
            await confirmedEmail.TrySendAsync(c.MemberId, theirs.Community.Name, by, ct);
        }

        return new VouchOutcomeDto(PersonDto.From(theirs, youVouched: true), nowConfirmed);
    }
}

/// <summary>Admin: confirm someone directly (the pilot's first people; a school with nobody to vouch yet).</summary>
/// <param name="CommunityId">The school.</param>
/// <param name="MemberId">Who.</param>
public sealed record AdminConfirmMemberCommand(Guid CommunityId, Guid MemberId) : IRequest<Result>;

/// <summary>Handles <see cref="AdminConfirmMemberCommand"/>: they must have joined the school; confirming twice
/// changes nothing. They get the "You're confirmed" email ("Confirmed by Msanga").</summary>
public sealed class AdminConfirmMemberCommandHandler(
    ICommunityMembershipRepository memberships,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ConfirmedEmail confirmedEmail) : IRequestHandler<AdminConfirmMemberCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(AdminConfirmMemberCommand c, CancellationToken ct)
    {
        var membership = await memberships.GetAsync(c.MemberId, c.CommunityId, ct);
        if (membership is null) return CommunityErrors.PersonNotFound;
        if (membership.Status == CommunityMembershipStatus.Confirmed) return Result.Success();

        membership.Confirm(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(ct);
        await confirmedEmail.TrySendAsync(c.MemberId, membership.Community.Name, [], ct);
        return Result.Success();
    }
}
