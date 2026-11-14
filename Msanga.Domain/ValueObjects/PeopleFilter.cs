namespace Msanga.Domain.ValueObjects;

/// <summary>What Find People looks for (all optional; together they narrow the search).</summary>
/// <param name="NameWords">Words of a name: each must be in the first name, last name or surname at school.</param>
/// <param name="SchoolId">Only this school (one of yours).</param>
/// <param name="SetYear">Only this set.</param>
/// <param name="House">House containing this ("blue" finds "Blue House").</param>
/// <param name="CountryCode">Lives in this country now (two letters).</param>
public sealed record PeopleFilter(
    IReadOnlyList<string> NameWords, Guid? SchoolId, int? SetYear, string? House, string? CountryCode);
