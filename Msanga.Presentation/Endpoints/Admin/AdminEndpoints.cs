using MediatR;
using Msanga.Application.Features.Communities.Commands;
using Msanga.Domain.Constants;
using Msanga.Presentation.Extensions;

namespace Msanga.Presentation.Endpoints.Admin;

/// <summary>
/// <c>/api/admin</c>: things only admins do. For now one: confirming someone directly (the pilot's first people, a
/// school with nobody to vouch yet). Admin screens come in M7; until then, Swagger.
/// </summary>
public static class AdminEndpoints
{
    /// <summary>Maps the routes.</summary>
    /// <param name="api">The <c>/api</c> group.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin").RequireAuthorization(Policies.RequireAdmin);

        admin.MapPost("/communities/{id:guid}/members/{memberId:guid}/confirm",
                async (Guid id, Guid memberId, ISender sender, CancellationToken ct) =>
                    (await sender.Send(new AdminConfirmMemberCommand(id, memberId), ct)).ToHttp())
            .WithName("AdminConfirmMember")
            .WithSummary("Confirm someone at a school directly (no vouches needed); they get the email")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }
}
