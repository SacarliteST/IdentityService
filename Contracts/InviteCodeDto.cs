namespace IdentityService.Contracts;

/// <summary>Код приглашения в административном списке.</summary>
public sealed record InviteCodeDto(
    Guid Id,
    string Code,
    string? Note,
    Guid? GroupId,
    string? GroupName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    int? MaxUses,
    int UsedCount,
    bool IsActive,
    Guid CreatedByUserId);

/// <summary>Создание кода приглашения.</summary>
/// <param name="Code">Текст кода; пустой — сгенерировать случайный.</param>
/// <param name="Note">Заметка для администратора, для кого код.</param>
/// <param name="ExpiresAt">Срок действия; <see langword="null"/> — без срока.</param>
/// <param name="MaxUses">Лимит использований; <see langword="null"/> — без ограничения.</param>
/// <param name="GroupId">Учебная группа из справочника, в которую попадёт зарегистрированный по коду (claim <c>group_name</c>).</param>
public sealed record CreateInviteCodeRequest(string? Code, string? Note, DateTimeOffset? ExpiresAt, int? MaxUses, Guid? GroupId = null);
