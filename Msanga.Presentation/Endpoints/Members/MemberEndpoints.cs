using MediatR;
using Msanga.Application.Features.Auth.Dtos;
using Msanga.Application.Features.Auth.Queries;
using Msanga.Application.Features.Communities.Dtos;
using Msanga.Application.Features.Communities.Queries;
using Msanga.Application.Features.Members.Commands;
using Msanga.Domain.Constants;
using Msanga.Presentation.Extensions;

namespace Msanga.Presentation.Endpoints.Members;

/// <summary><c>/api/me</c>: the signed-in member, editing their profile, and their schools.</summary>
public static class MemberEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder api)
    {
        var me = api.MapGroup("/me").WithTags("Me").RequireAuthorization(Policies.RequireMember);

        me.MapGet("", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMeQuery(), ct)).ToHttp())
            .WithName("GetMe")
            .WithSummary("The signed-in member (the web app calls this on start-up to check its session)")
            .Produces<MemberDto>();

        me.MapPut("", async (UpdateMyProfileCommand command, ISender sender, CancellationToken ct) =>
                (await sender.Send(command, ct)).ToHttp())
            .WithName("UpdateMyProfile")
            .WithSummary("Edit my profile: names, surname at school, a line about me, where I live now (optional)")
            .Produces<MemberDto>()
            .ProducesValidationProblem();

        me.MapGet("/schools", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMySchoolsQuery(), ct)).ToHttp())
            .WithName("GetMySchools")
            .WithSummary("My Schools: the schools I joined, with my years, most recent first")
            .Produces<IReadOnlyList<MembershipDto>>();

        return api;
    }
}
