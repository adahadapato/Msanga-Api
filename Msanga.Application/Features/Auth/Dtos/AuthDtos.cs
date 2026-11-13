using Msanga.Domain.Interfaces;

namespace Msanga.Application.Features.Auth.Dtos;

/// <summary>A signed-in session, as the app receives it.</summary>
/// <param name="AccessToken">Send as "Authorization: Bearer …".</param>
/// <param name="AccessTokenExpiresAt">When to refresh (UTC).</param>
/// <param name="RefreshToken">Swaps for a new pair at <c>/api/auth/refresh</c>.</param>
/// <param name="RefreshTokenExpiresAt">When the session ends for good (UTC).</param>
/// <param name="Member">Who is signed in.</param>
public sealed record AuthTokensDto(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    MemberDto Member)
{
    /// <summary>Maps the tokens.</summary>
    /// <param name="t">The tokens.</param>
    /// <returns>The DTO.</returns>
    public static AuthTokensDto From(AuthTokens t) =>
        new(t.AccessToken, t.AccessTokenExpiresAt, t.RefreshToken, t.RefreshTokenExpiresAt, MemberDto.From(t.Account));
}

/// <summary>
/// The signed-in member, exactly as the web app's <c>Member</c> type (src/api/authTypes.ts). No popularity counts,
/// by design.
/// </summary>
/// <param name="Id">The member's Id.</param>
/// <param name="Email">Their email (only they see it).</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="SchoolSurname">Surname at school; null = none.</param>
/// <param name="DisplayName">"Hilary A.".</param>
/// <param name="MainSet">The set shown under their name, e.g. "Class of 1998"; null until sets exist (M2–M3).</param>
/// <param name="Bio">About them; empty = none.</param>
/// <param name="Photo">Their picture's address; null = initials.</param>
public sealed record MemberDto(
    Guid Id, string Email, string FirstName, string LastName, string? SchoolSurname, string DisplayName,
    string? MainSet, string Bio, string? Photo)
{
    /// <summary>Maps an account.</summary>
    /// <param name="a">The account.</param>
    /// <returns>The DTO.</returns>
    public static MemberDto From(MemberAccount a) =>
        new(a.Id, a.Email, a.FirstName, a.LastName, a.SchoolSurname, a.DisplayName, MainSet: null, a.Bio, a.PhotoUrl);
}
