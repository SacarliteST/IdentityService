namespace IdentityService.Web.Common;

/// <summary>
/// Контракт вертикального слайса эндпоинта. Каждый класс-реализация
/// регистрирует свои маршруты в <see cref="MapEndpoints"/>.
/// Все реализации автоматически обнаруживаются и регистрируются через
/// <see cref="EndpointExtensions.AddEndpoints"/>.
/// </summary>
public interface IEndpoint
{
    /// <summary>Регистрирует маршруты в роутинге ASP.NET Core.</summary>
    void MapEndpoints(IEndpointRouteBuilder app);
}
