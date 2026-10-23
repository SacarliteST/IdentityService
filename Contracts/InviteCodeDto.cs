namespace IdentityService.Contracts;

/// <summary>Код приглашения в административном списке.</summary>
public sealed record InviteCodeDto(
    Guid Id,
    string Code,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    int? MaxUses,
    int UsedCount,
    bool IsActive,
    Guid CreatedByUserId);

/// <summary>Создание кода приглашения. Пустой <paramref name="Code"/> — сгенерировать случайный.</summary>
public sealed record CreateInviteCodeRequest(string? Code, string? Note, DateTimeOffset? ExpiresAt, int? MaxUses);
