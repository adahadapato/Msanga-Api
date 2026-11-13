using MediatR;
using Msanga.Application.Features.Auth.Dtos;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Shared.Results;

namespace Msanga.Application.Features.Auth.Queries;

/// <summary>Who the signed-in member is (the web app calls this on start-up to check its session).</summary>
public sealed record GetMeQuery : IRequest<Result<MemberDto>>;

/// <summary>Handles <see cref="GetMeQuery"/>.</summary>
public sealed class GetMeQueryHandler(IAuthService auth, ICurrentUser currentUser)
    : IRequestHandler<GetMeQuery, Result<MemberDto>>
{
    /// <inheritdoc />
    public async Task<Result<MemberDto>> Handle(GetMeQuery query, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");
        return (await auth.GetAccountAsync(userId, ct)).Map(MemberDto.From);
    }
}
