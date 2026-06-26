namespace IdentityService.Host;

/// <summary>
/// Маркер сборки Host. Используется как якорь для рефлексии:
/// сканирование эндпоинтов, регистрация валидаторов FluentValidation,
/// а также как точка входа для <c>WebApplicationFactory&lt;IHostMarker&gt;</c> в тестах.
/// </summary>
public interface IHostMarker;
