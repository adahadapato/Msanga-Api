using MediatR;
using Msanga.Application.Features.Communities.Commands;
using Msanga.Application.Features.Communities.Dtos;
using Msanga.Application.Features.Communities.Queries;
using Msanga.Domain.Constants;
using Msanga.Presentation.Extensions;
using Msanga.Presentation.Settings.RateLimiting;

namespace Msanga.Presentation.Endpoints.Communities;

/// <summary>
/// <c>/api/communities</c>: finding a school, opening its page, adding one, and "I was a student here" (joining,
/// changing your years, leaving). Signed-in members only.
/// </summary>
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

        communities.MapPost("", async (AddCommunityCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                if (result.IsFailure) return result.Error.ToProblem();

                var outcome = result.Value;
                return outcome.Added is { } added
                    ? Results.Created($"/api/communities/{added.Id}", added)
                    : outcome.Problem!.ToProblem(new Dictionary<string, object?> { ["suggestions"] = outcome.Suggestions });
            })
            .WithName("AddCommunity")
            .WithSummary("Add a school (marked new). 409 Community.AlreadyListed or Community.LooksLike, with " +
                         "\"suggestions\"; send again with addAnyway: true if it's none of them")
            .Produces<CommunityDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRateLimiting(RateLimitingSetup.AddCommunityPolicy);

        // ---------- "I was a student here": the signed-in member's membership of one school ----------

        var membership = communities.MapGroup("/{id:guid}/membership");

        membership.MapGet("", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetMyMembershipQuery(id), ct)).ToHttp())
            .WithName("GetMyMembership")
            .WithSummary("Have I joined this school? My years there; 404 Community.NotAMember if not")
            .Produces<MembershipDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        membership.MapPost("", async (Guid id, SchoolYearsBody body, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new JoinCommunityCommand(id, body.FromYear, body.ToYear, body.Finished), ct);
                return result.IsSuccess
                    ? Results.Created($"/api/communities/{id}/membership", result.Value)
                    : result.Error.ToProblem();
            })
            .WithName("JoinCommunity")
            .WithSummary("I was a student here: join the school with my years (status Joined; 409 if already joined)")
            .Produces<MembershipDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        membership.MapPut("", async (Guid id, SchoolYearsBody body, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ChangeMySchoolYearsCommand(id, body.FromYear, body.ToYear, body.Finished), ct)).ToHttp())
            .WithName("ChangeMySchoolYears")
            .WithSummary("Correct my years at this school")
            .Produces<MembershipDto>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        membership.MapDelete("", async (Guid id, ISender sender, CancellationToken ct) =>
                (await sender.Send(new LeaveCommunityCommand(id), ct)).ToHttp())
            .WithName("LeaveCommunity")
            .WithSummary("Leave this school (my membership is deleted)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }
}

/// <summary>My years at a school, as the form sends them.</summary>
/// <param name="FromYear">The year I started.</param>
/// <param name="ToYear">The year I left.</param>
/// <param name="Finished">True if I finished there.</param>
public sealed record SchoolYearsBody(int FromYear, int ToYear, bool Finished);
