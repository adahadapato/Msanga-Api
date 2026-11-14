using MediatR;
using Msanga.Application.Features.Communities.Dtos;
using Msanga.Domain.Interfaces;
using Msanga.Domain.Shared;
using Msanga.Domain.Shared.Errors;
using Msanga.Domain.Shared.Results;

namespace Msanga.Application.Features.Communities.Queries;

/// <summary>Find a school by what the member typed: "fgc ikot", "UNILAG", "Govt. Coll. Umuahia".</summary>
/// <param name="Search">What was typed.</param>
public sealed record SearchCommunitiesQuery(string? Search) : IRequest<Result<IReadOnlyList<CommunityDto>>>
{
    /// <summary>Longest search text.</summary>
    public const int SearchMaxLength = 100;

    /// <summary>The most results (best first).</summary>
    public const int MaxResults = 20;

    /// <summary>Fewer letters than this in the search key gives no results, not the first 20 of everything.</summary>
    public const int MinKeyLength = 2;
}

/// <summary>Handles <see cref="SearchCommunitiesQuery"/>.</summary>
public sealed class SearchCommunitiesQueryHandler(ICommunityRepository communities)
    : IRequestHandler<SearchCommunitiesQuery, Result<IReadOnlyList<CommunityDto>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CommunityDto>>> Handle(SearchCommunitiesQuery query, CancellationToken ct)
    {
        var words = SearchKey.Words(query.Search);
        if (string.Join(' ', words).Length < SearchCommunitiesQuery.MinKeyLength)
            return Result.Success<IReadOnlyList<CommunityDto>>([]);

        var found = await communities.SearchAsync(words, SearchCommunitiesQuery.MaxResults, ct);
        return Result.Success<IReadOnlyList<CommunityDto>>(found.Select(CommunityDto.From).ToList());
    }
}

/// <summary>One school, for its page.</summary>
/// <param name="Id">The school's Id.</param>
public sealed record GetCommunityQuery(Guid Id) : IRequest<Result<CommunityDto>>;

/// <summary>Handles <see cref="GetCommunityQuery"/>.</summary>
public sealed class GetCommunityQueryHandler(ICommunityRepository communities)
    : IRequestHandler<GetCommunityQuery, Result<CommunityDto>>
{
    /// <inheritdoc />
    public async Task<Result<CommunityDto>> Handle(GetCommunityQuery query, CancellationToken ct)
    {
        var community = await communities.GetByIdAsync(query.Id, ct);
        return community is null ? CommunityErrors.NotFound : CommunityDto.From(community);
    }
}
