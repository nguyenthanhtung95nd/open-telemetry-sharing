namespace Identity.Api.Keycloak;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string BaseUrl { get; init; } = string.Empty;
    public string Realm { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string CustomerRoleName { get; init; } = string.Empty;
}
