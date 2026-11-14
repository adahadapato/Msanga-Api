using MediatR;
using Msanga.Application.Features.Communities.Dtos;
using Msanga.Domain.Constants;
using Msanga.Domain.Entities;
using Msanga.Domain.Enums;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Shared;
using Msanga.Domain.Shared.Errors;
using Msanga.Domain.Shared.Results;

namespace Msanga.Application.Features.Communities.Commands;

/// <summary>A member adds a school that isn't on Msanga yet. It's marked "new" until an admin checks it (M7).</summary>
/// <param name="Name">Official name, e.g. "Holy Child College".</param>
/// <param name="Level">Primary, Secondary or Tertiary.</param>
/// <param name="Town">Town or city.</param>
/// <param name="State">Nigeria: one of the 36 states or the FCT. Elsewhere: state or region, optional.</param>
/// <param name="CountryCode">Two-letter country code; "NG" for Nigeria.</param>
/// <param name="OtherNames">Up to 5 other names it's known by ("HCC"); optional.</param>
/// <param name="AddAnyway">True after the member has seen the lookalikes and said theirs is different.</param>
public sealed record AddCommunityCommand(
    string Name, CommunityLevel Level, string Town, string? State, string CountryCode,
    IReadOnlyList<string>? OtherNames, bool AddAnyway = false) : IRequest<Result<AddCommunityOutcome>>
{
    /// <summary>The most other names when adding.</summary>
    public const int MaxOtherNames = 5;

    /// <summary>The most lookalikes shown.</summary>
    public const int MaxSuggestions = 5;
}

/// <summary>
/// What happened: <see cref="Added"/> (201), or a <see cref="Problem"/> (409 <c>Community.AlreadyListed</c> /
/// <c>Community.LooksLike</c>) with the schools to show the member.
/// </summary>
/// <param name="Added">The new school; null if it wasn't added.</param>
/// <param name="Problem">Why it wasn't added; null if it was.</param>
/// <param name="Suggestions">The school that's already there, or the lookalikes; empty if it was added.</param>
public sealed record AddCommunityOutcome(CommunityDto? Added, Error? Problem, IReadOnlyList<CommunityDto> Suggestions)
{
    /// <summary>Added.</summary>
    /// <param name="community">The new school.</param>
    /// <returns>The outcome.</returns>
    public static AddCommunityOutcome WasAdded(CommunityDto community) => new(community, null, []);

    /// <summary>Not added: this one is already on Msanga.</summary>
    /// <param name="existing">The school that's there.</param>
    /// <returns>The outcome.</returns>
    public static AddCommunityOutcome AlreadyListed(CommunityDto existing) =>
        new(null, CommunityErrors.AlreadyListed, [existing]);

    /// <summary>Not added yet: is it one of these?</summary>
    /// <param name="lookalikes">Schools that look like it.</param>
    /// <returns>The outcome.</returns>
    public static AddCommunityOutcome LooksLike(IReadOnlyList<CommunityDto> lookalikes) =>
        new(null, CommunityErrors.LooksLike, lookalikes);
}

/// <summary>Handles <see cref="AddCommunityCommand"/>.</summary>
/// <remarks>
/// <para><b>1. Already there?</b> Same search key (name and town, however written) → AlreadyListed; no way round it.</para>
/// <para><b>2. Lookalikes?</b> Unless <c>AddAnyway</c>: a search with the new school's NAME (not its town, so "Kings
/// College" in "Lagos Island" still finds King's College, Lagos), then with each of its other names → LooksLike with
/// up to 5.</para>
/// <para><b>3. Add</b> it as New, with who added it.</para>
/// </remarks>
public sealed class AddCommunityCommandHandler(
    ICommunityRepository communities,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IRequestHandler<AddCommunityCommand, Result<AddCommunityOutcome>>
{
    /// <inheritdoc />
    public async Task<Result<AddCommunityOutcome>> Handle(AddCommunityCommand c, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new InvalidOperationException("This action requires a signed-in user.");

        var country = c.CountryCode.Trim().ToUpperInvariant();
        var state = country == Community.Nigeria ? NigerianStates.Find(c.State) : c.State;

        var community = Community.Create(c.Name, c.Level, c.Town, state, country, CommunityStatus.New, userId);
        foreach (var otherName in c.OtherNames ?? [])
            community.AddAlias(otherName);

        // 1. Already there?
        var existing = await communities.FindBySearchKeyAsync(community.SearchKey, ct);
        if (existing is not null)
            return AddCommunityOutcome.AlreadyListed(CommunityDto.From(existing));

        // 2. Lookalikes?
        if (!c.AddAnyway)
        {
            var lookalikes = await LookalikesAsync(community, ct);
            if (lookalikes.Count > 0)
                return AddCommunityOutcome.LooksLike(lookalikes);
        }

        // 3. Add
        communities.Add(community);
        await unitOfWork.SaveChangesAsync(ct);
        return AddCommunityOutcome.WasAdded(CommunityDto.From(community));
    }

    /// <summary>Schools found by the new name's words, then by each other name's, without repeats.</summary>
    private async Task<IReadOnlyList<CommunityDto>> LookalikesAsync(Community community, CancellationToken ct)
    {
        var keys = new[] { SearchKey.For(community.Name) }.Concat(community.Aliases.Select(a => a.SearchKey));
        var found = new List<Community>();

        foreach (var key in keys)
        {
            var words = key.Split(' ');
            foreach (var match in await communities.SearchAsync(words, AddCommunityCommand.MaxSuggestions, ct))
            {
                if (found.All(f => f.Id != match.Id))
                    found.Add(match);
            }
        }

        return found.Take(AddCommunityCommand.MaxSuggestions).Select(CommunityDto.From).ToList();
    }
}
