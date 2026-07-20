using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityService.Contracts;

namespace IdentityService.Client;

/// <summary>Реализация <see cref="IIdentityClient"/> поверх <see cref="HttpClient"/>.</summary>
public sealed class IdentityClient(HttpClient httpClient) : IIdentityClient
{
    public async Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(ApiRoutes.Auth.Register, request, ClientJson.Default, ct);
        return await response.Content.ReadFromJsonAsync<TokenResponse>(ClientJson.Default, ct)
               ?? throw new InvalidOperationException("Empty response from Register.");
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(ApiRoutes.Auth.Login, request, ClientJson.Default, ct);
        return await response.Content.ReadFromJsonAsync<TokenResponse>(ClientJson.Default, ct)
               ?? throw new InvalidOperationException("Empty response from Login.");
    }

    public async Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(ApiRoutes.Auth.Refresh, request, ClientJson.Default, ct);
        return await response.Content.ReadFromJsonAsync<TokenResponse>(ClientJson.Default, ct)
               ?? throw new InvalidOperationException("Empty response from Refresh.");
    }

    public async Task LogoutAsync(RefreshRequest request, CancellationToken ct = default)
    {
        await httpClient.PostAsJsonAsync(ApiRoutes.Auth.Logout, request, ClientJson.Default, ct);
    }

    public async Task<UserDetailsDto> CreateUserAsync(
        CreateUserRequest request,
        string bearerToken,
        CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, ApiRoutes.Users.List);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        req.Content = JsonContent.Create(request, options: ClientJson.Default);
        var response = await httpClient.SendAsync(req, ct);
        return await response.Content.ReadFromJsonAsync<UserDetailsDto>(ClientJson.Default, ct)
            ?? throw new InvalidOperationException("Empty response from CreateUser.");
    }

    public async Task UpdateUserRolesAsync(
        Guid userId,
        UpdateUserRolesRequest request,
        string bearerToken,
        CancellationToken ct = default)
    {
        var path = $"{ApiRoutes.PrefixV1}/users/{userId}/roles";
        using var req = new HttpRequestMessage(HttpMethod.Put, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        req.Content = JsonContent.Create(request, options: ClientJson.Default);
        await httpClient.SendAsync(req, ct);
    }
}
