using System.Text.Json.Serialization;

namespace Identity.Api.Keycloak.Models;

internal sealed record AdminAccessToken(
    [property: JsonPropertyName("access_token")] string AccessToken);

internal sealed record KeycloakCredential(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("temporary")] bool Temporary);

internal sealed record CreateUserPayload(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("firstName")] string FirstName,
    [property: JsonPropertyName("lastName")] string LastName,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("emailVerified")] bool EmailVerified,
    [property: JsonPropertyName("credentials")] KeycloakCredential[] Credentials);

internal sealed record UpdateUserPayload(
    [property: JsonPropertyName("email")] string? Email = null,
    [property: JsonPropertyName("firstName")] string? FirstName = null,
    [property: JsonPropertyName("lastName")] string? LastName = null,
    [property: JsonPropertyName("enabled")] bool? Enabled = null);

internal sealed record RealmRole(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

internal sealed record KeycloakUser(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("firstName")] string? FirstName,
    [property: JsonPropertyName("lastName")] string? LastName,
    [property: JsonPropertyName("enabled")] bool Enabled);
