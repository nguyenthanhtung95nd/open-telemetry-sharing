namespace Bookmarks.Api.Common;

/// <summary>
/// Why a service call failed, in terms the HTTP layer can map to a status code without
/// knowing anything about the operation that produced it.
/// </summary>
public enum ResultErrorKind
{
    /// <summary>No failure.</summary>
    None = 0,

    /// <summary>The request was understood but its contents were not acceptable.</summary>
    Validation = 1,

    /// <summary>The addressed resource does not exist.</summary>
    NotFound = 2,

    /// <summary>The request conflicts with the current state, such as a duplicate name.</summary>
    Conflict = 3
}
