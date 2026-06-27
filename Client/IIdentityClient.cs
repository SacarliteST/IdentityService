using IdentityService.Contracts;

namespace IdentityService.Client;

/// <summary>
/// Типизированный HTTP-клиент для IdentityService.
/// Ошибки (401/403/409/422) выбрасываются как типизированные исключения через <see cref="ErrorDelegatingHandler"/>.
/// </summary>
public interface IIdentityClient
{
    /// <summary>Регистрирует нового пользователя и возвращает пару токенов.</summary>
    Task<TokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>Аутентифицирует пользователя и возвращает пару токенов.</summary>
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>Ротирует refresh-токен, возвращает новую пару токенов.</summary>
    Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);

    /// <summary>Отзывает refresh-токен (выход). Идемпотентно.</summary>
    Task LogoutAsync(RefreshRequest request, CancellationToken ct = default);

    /// <summary>Присваивает роль пользователю. Требует Bearer-токен Admin.</summary>
    Task AssignRoleAsync(Guid userId, AssignRoleRequest request, string bearerToken, CancellationToken ct = default);
}
