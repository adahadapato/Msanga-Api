using MediatR;
using Msanga.Application.Features.Communities.Dtos;
using Msanga.Application.Features.Communities.Queries;
using Msanga.Domain.Constants;
using Msanga.Presentation.Extensions;

namespace Msanga.Presentation.Endpoints.Communities;

/// <summary><c>/api/communities</c>: finding a school and opening its page. Signed-in members only.</summary>
public static class CommunityEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapCommunityEndpoints(this IEndpointRouteBuilder api)
    {
        var communities = api.MapGroup("/communities").WithTags("Communities")
                             .RequireAuthorization(Policies.RequireMember);

        communities.MapGet("", async (string? search, ISender sender, CancellationToken ct) =>
                (await sender.Send(new SearchCommunitiesQuery(search), ct)).ToHttp())
            .WithName("SearchCommunities")
            .WithSummary("Find a school: \"fgc ikot\", \"UNILAG\", \"Govt. Coll. Umuahia\" (best first, at most 20)")
            .Produces<IReadOnlyList<CommunityDto>>()
            .ProducesValidationProblem();

        communities.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetCommunityQuery(id), ct)).ToHttp())
            .WithName("GetCommunity")
            .WithSummary("One school, for its page")
            .Produces<CommunityDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }
}
