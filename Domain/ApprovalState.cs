namespace IdentityService.Domain;

/// <summary>Состояние заявки на регистрацию. Существующие и созданные админом пользователи всегда <see cref="Approved"/>.</summary>
public enum ApprovalState
{
    /// <summary>Доступ разрешён.</summary>
    Approved = 0,

    /// <summary>Заявка ждёт решения администратора, войти нельзя.</summary>
    Pending = 1,

    /// <summary>Заявка отклонена, войти нельзя (администратор может одобрить позже).</summary>
    Rejected = 2,
}
