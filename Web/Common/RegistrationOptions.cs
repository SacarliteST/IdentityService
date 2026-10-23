namespace IdentityService.Web.Common;

/// <summary>Режим самостоятельной регистрации.</summary>
public enum RegistrationMode
{
    /// <summary>Заявка ждёт одобрения администратора; код приглашения одобряет сразу.</summary>
    Approval = 0,

    /// <summary>Любой зарегистрировавшийся сразу получает доступ (прежнее поведение).</summary>
    Open = 1,

    /// <summary>Регистрация выключена.</summary>
    Closed = 2,
}

/// <summary>Настройки секции <c>Registration</c>.</summary>
public sealed class RegistrationOptions
{
    public const string SectionKey = "Registration";

    public RegistrationMode Mode { get; set; } = RegistrationMode.Approval;
}
