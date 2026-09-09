using System.Security.Claims;

namespace IdentityService.Web.Common.Tokens;

/// <summary>
/// Валидирует токен, предъявляемый как <c>subjectToken</c> в Token Exchange —
/// теми же правилами (подпись/issuer/audience/lifetime), что и обычный JWT-bearer
/// на входящих запросах (см. <see cref="JwtBearerPostConfigure"/>), но синхронно,
/// вне ASP.NET auth middleware.
/// </summary>
internal interface ISubjectTokenValidator
{
    /// <summary>Возвращает claims токена при успешной валидации, иначе <c>null</c>.</summary>
    Task<ClaimsPrincipal?> ValidateAsync(string token, CancellationToken ct = default);
}
