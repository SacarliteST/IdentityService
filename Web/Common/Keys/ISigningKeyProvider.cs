using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Web.Common.Keys;

/// <summary>
/// Управляет RSA-ключом подписи JWT. Синглтон: ключ загружается один раз при старте.
/// Приватный ключ не покидает сервис — наружу отдаётся только публичная часть через <see cref="GetPublicJwk"/>.
/// </summary>
public interface ISigningKeyProvider
{
    /// <summary>Стабильный идентификатор ключа (key ID). Переживает рестарты при использовании файла/конфига.</summary>
    string Kid { get; }

    /// <summary>
    /// Возвращает <see cref="SigningCredentials"/> с приватным ключом для подписи access-токенов.
    /// Используется только внутри сервиса при выпуске токенов.
    /// </summary>
    SigningCredentials GetSigningCredentials();

    /// <summary>
    /// Возвращает публичную часть ключа в формате JWK (только <c>n</c> и <c>e</c>, без <c>d/p/q</c>).
    /// Публикуется через эндпоинт <c>/.well-known/jwks.json</c> для валидации токенов в клиентских сервисах.
    /// </summary>
    JsonWebKey GetPublicJwk();
}
