using MediatR;
using Msanga.Application.Features.People.Dtos;
using Msanga.Application.Features.People.Queries;
using Msanga.Domain.Constants;
using Msanga.Presentation.Extensions;
using Msanga.Presentation.Settings.RateLimiting;

namespace Msanga.Presentation.Endpoints.People;

/// <summary>
/// <c>/api/people</c>: Find People and a person's page (M3d). Only people confirmed at schools where you're confirmed;
/// nobody else is ever found or shown.
/// </summary>
public static class PeopleEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapPeopleEndpoints(this IEndpointRouteBuilder api)
    {
        var people = api.MapGroup("/people").WithTags("People")
                        .RequireAuthorization(Policies.RequireMember)
                        .RequireRateLimiting(RateLimitingSetup.PeoplePolicy);

        people.MapGet("", async (string? search, Guid? school, int? set, string? house, string? country,
                                 ISender sender, CancellationToken ct) =>
                (await sender.Send(new FindPeopleQuery(search, school, set, house, country), ct)).ToHttp())
            .WithName("FindPeople")
            .WithSummary("Find people confirmed at your schools: ?search=okon&school=…&set=1998&house=blue&country=GB " +
                         "(A–Z, at most 50); 403 People.ConfirmFirst if you aren't confirmed anywhere")
            .Produces<IReadOnlyList<PersonCardDto>>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        people.MapGet("/{memberId:guid}", async (Guid memberId, ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetPersonQuery(memberId), ct)).ToHttp())
            .WithName("GetPerson")
            .WithSummary("A person's page: name, bio, where they live, the schools you share; 404 for anyone else")
            .Produces<PersonPageDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }
}
