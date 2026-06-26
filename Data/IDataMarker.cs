namespace IdentityService.Data;

/// <summary>
/// Маркер сборки Data. Используется как якорь для рефлексии:
/// <c>builder.ApplyConfigurationsFromAssembly(typeof(IDataMarker).Assembly)</c>
/// подхватывает все <c>IEntityTypeConfiguration</c> в проекте.
/// </summary>
public interface IDataMarker;
