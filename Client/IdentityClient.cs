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

    public async Task BlockUserAsync(
        Guid userId,
        BlockUserRequest request,
        string bearerToken,
        CancellationToken ct = default)
    {
        var path = $"{ApiRoutes.PrefixV1}/users/{userId}/block";
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        req.Content = JsonContent.Create(request, options: ClientJson.Default);
        await httpClient.SendAsync(req, ct);
    }

    public async Task UnblockUserAsync(
        Guid userId,
        string bearerToken,
        CancellationToken ct = default)
    {
        var path = $"{ApiRoutes.PrefixV1}/users/{userId}/unblock";
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        await httpClient.SendAsync(req, ct);
    }

    public Task<PagedResponse<AuditEventDto>> GetUserActivityAsync(
        Guid userId,
        GetUserActivityRequest request,
        string bearerToken,
        CancellationToken ct = default)
    {
        var path = $"{ApiRoutes.PrefixV1}/users/{userId}/activity";
        var query = BuildAuditQuery(
            request.Page,
            request.PageSize,
            null,
            null,
            request.EventType,
            request.From,
            request.To);
        return SendAuthorizedGetAsync<PagedResponse<AuditEventDto>>(path + query, bearerToken, ct);
    }

    public Task<PagedResponse<AuditEventDto>> GetAuditEventsAsync(
        GetAuditEventsRequest request,
        string bearerToken,
        CancellationToken ct = default)
    {
        var query = BuildAuditQuery(
            request.Page,
            request.PageSize,
            request.ActorUserId,
            request.TargetUserId,
            request.EventType,
            request.From,
            request.To);
        return SendAuthorizedGetAsync<PagedResponse<AuditEventDto>>(
            ApiRoutes.Audit.List + query,
            bearerToken,
            ct);
    }

    private async Task<T> SendAuthorizedGetAsync<T>(
        string path,
        string bearerToken,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        var response = await httpClient.SendAsync(req, ct);
        return await response.Content.ReadFromJsonAsync<T>(ClientJson.Default, ct)
            ?? throw new InvalidOperationException($"Empty response from {path}.");
    }

    private static string BuildAuditQuery(
        int? page,
        int? pageSize,
        Guid? actorUserId,
        Guid? targetUserId,
        string? eventType,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var parameters = new List<string>();
        Add("page", page?.ToString());
        Add("pageSize", pageSize?.ToString());
        Add("actorUserId", actorUserId?.ToString());
        Add("targetUserId", targetUserId?.ToString());
        Add("eventType", eventType);
        Add("from", from?.ToString("O"));
        Add("to", to?.ToString("O"));
        return parameters.Count == 0 ? String.Empty : $"?{String.Join("&", parameters)}";

        void Add(string name, string? value)
        {
            if (!String.IsNullOrWhiteSpace(value))
            {
                parameters.Add($"{name}={Uri.EscapeDataString(value)}");
            }
        }
    }
}
