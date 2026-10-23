namespace IdentityService.Contracts;

/// <summary>Статусы доступа пользователя.</summary>
public static class UserStatuses
{
    public const string Active = "Active";
    public const string Blocked = "Blocked";

    /// <summary>Заявка на регистрацию ждёт решения администратора.</summary>
    public const string Pending = "Pending";

    /// <summary>Заявка на регистрацию отклонена.</summary>
    public const string Rejected = "Rejected";
}
