namespace IdentityService.Contracts;

/// <summary>Учебная группа из справочника.</summary>
public sealed record GroupDto(Guid Id, string Name, DateTimeOffset CreatedAt);

/// <summary>Создание или переименование группы.</summary>
/// <param name="Name">Название группы, уникальное; до 50 символов.</param>
public sealed record SaveGroupRequest(string Name);
