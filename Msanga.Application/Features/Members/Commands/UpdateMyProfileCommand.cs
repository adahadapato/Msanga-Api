using MediatR;
using Msanga.Application.Features.Auth.Dtos;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Shared.Errors;
using Msanga.Domain.Shared.Results;

namespace Msanga.Application.Features.Members.Commands;

/// <summary>Edit your profile: names, surname at school, a line about you. Photos come with uploads.</summary>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="SchoolSurname">Surname at school; blank = none.</param>
/// <param name="Bio">About you; blank = none.</param>
/// <param name="LivesInCity">Where you live now, town or city; blank = none (optional, schoolmates only).</param>
/// <param name="LivesInCountryCode">Where you live now, country (two letters); blank = none.</param>
public sealed record UpdateMyProfileCommand(string FirstName, string LastName, string? SchoolSurname, string? Bio,
                                            string? LivesInCity = null, string? LivesInCountryCode = null)
    : IRequest<Result<MemberDto>>;

/// <summary>Handles <see cref="UpdateMyProfileCommand"/>: changes the signed-in member only.</summary>
public sealed class UpdateMyProfileCommandHandler(
    IMemberRepository members,
    IUnitOfWork unitOfWork,
    IAuthService auth,
    ICurrentUser currentUser) : IRequestHandler<UpdateMyProfileCommand, Result<MemberDto>>
{
    /// <inheritdoc />
    public async Task<Result<MemberDto>> Handle(UpdateMyProfileCommand c, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var member = await members.GetByIdAsync(userId, ct);
        if (member is null) return AuthErrors.UserNotFound;

        member.UpdateProfile(c.FirstName, c.LastName, c.SchoolSurname, c.Bio);
        member.SetWhereYouLive(c.LivesInCity, c.LivesInCountryCode);
        await unitOfWork.SaveChangesAsync(ct);

        return (await auth.GetAccountAsync(userId, ct)).Map(MemberDto.From);
    }
}
