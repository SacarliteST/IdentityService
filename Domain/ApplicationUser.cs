using Microsoft.AspNetCore.Identity;

namespace IdentityService.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }

    /// <summary>Имя. Источник истины для учебных сервисов (приходит в токене claim'ом <c>given_name</c>).</summary>
    public string? FirstName { get; set; }

    /// <summary>Фамилия (claim <c>family_name</c>).</summary>
    public string? LastName { get; set; }

    /// <summary>Отчество (claim <c>middle_name</c>).</summary>
    public string? MiddleName { get; set; }

    /// <summary>Решение по заявке на регистрацию; доступ есть только у <see cref="ApprovalState.Approved"/>.</summary>
    public ApprovalState ApprovalState { get; set; } = ApprovalState.Approved;

    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>Кто одобрил заявку; <see langword="null"/> — одобрена автоматически (код приглашения, открытый режим).</summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>Код приглашения, с которым зарегистрировался пользователь.</summary>
    public Guid? InviteCodeId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset? BlockedAt { get; set; }

    public string? BlockReason { get; set; }
}
