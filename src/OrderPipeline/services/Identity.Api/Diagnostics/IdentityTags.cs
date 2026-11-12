namespace Identity.Api.Diagnostics;

/// <summary>Span attribute names used by this service.</summary>
/// <remarks>
/// Accounts are identified by their Keycloak subject id. A username or an email address is
/// personally identifiable and must never be written to a span.
/// </remarks>
public static class IdentityTags
{
    /// <summary>Keycloak subject id of the account a span is acting on.</summary>
    public const string UserId = "enduser.id";
}
