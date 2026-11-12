using System.Net.Http.Json;
using Identity.Api.Keycloak.Models;
using Microsoft.Extensions.Options;

namespace Identity.Api.Keycloak;

public sealed class KeycloakAdminHttp(HttpClient httpClient, IOptions<KeycloakOptions> options)
{
    private readonly KeycloakOptions _options = options.Value;
    private string? _cachedToken;

    public string Realm => _options.Realm;

    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, object? payload = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(method, path);
        if (payload is not null) request.Content = JsonContent.Create(payload);
        request.Headers.Authorization = new("Bearer", await GetTokenAsync(ct));

        return await httpClient.SendAsync(request, ct);
    }

    public async Task<T?> ReadAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await SendAsync(HttpMethod.Get, path, null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(ct);
    }

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null) return _cachedToken;

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret
        };

        var response = await httpClient.PostAsync(
            $"/realms/{_options.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(parameters),
            ct);

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<AdminAccessToken>(ct);
        _cachedToken = token!.AccessToken;
        return _cachedToken;
    }
}
