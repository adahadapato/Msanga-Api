namespace Msanga.Domain.Shared.Errors;

/// <summary>What kind of failure an <see cref="Error"/> is; Presentation turns each into an HTTP status.</summary>
public enum ErrorType
{
    Failure = 0,      // 500 / 400: something went wrong
    Validation = 1,   // 400: the input is wrong ("Price must be more than zero")
    NotFound = 2,     // 404: "Member not found"
    Conflict = 3,     // 409: "That email is already in use"
    Forbidden = 4,    // 403: "Please confirm your email first"
    Unauthorized = 5  // 401: not signed in, or the session has expired
}