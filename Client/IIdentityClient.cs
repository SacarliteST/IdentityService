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

    /// <summary>Создаёт пользователя без выдачи токенов. Требует Bearer-токен Admin.</summary>
    Task<UserDetailsDto> CreateUserAsync(
        CreateUserRequest request,
        string bearerToken,
        CancellationToken ct = default);

    /// <summary>Атомарно заменяет роли пользователя. Требует Bearer-токен Admin.</summary>
    Task UpdateUserRolesAsync(
        Guid userId,
        UpdateUserRolesRequest request,
        string bearerToken,
        CancellationToken ct = default);

    /// <summary>Блокирует пользователя и отзывает его refresh-токены.</summary>
    Task BlockUserAsync(
        Guid userId,
        BlockUserRequest request,
        string bearerToken,
        CancellationToken ct = default);

    /// <summary>Снимает административную блокировку пользователя.</summary>
    Task UnblockUserAsync(
        Guid userId,
        string bearerToken,
        CancellationToken ct = default);

    /// <summary>Возвращает постраничную активность пользователя.</summary>
    Task<PagedResponse<AuditEventDto>> GetUserActivityAsync(
        Guid userId,
        GetUserActivityRequest request,
        string bearerToken,
        CancellationToken ct = default);

    /// <summary>Возвращает постраничный журнал аудита.</summary>
    Task<PagedResponse<AuditEventDto>> GetAuditEventsAsync(
        GetAuditEventsRequest request,
        string bearerToken,
        CancellationToken ct = default);
}
