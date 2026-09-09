namespace IdentityService.Contracts;

public static class AuditEventTypes
{
    public const string LoginSucceeded = "LoginSucceeded";
    public const string UserCreated = "UserCreated";
    public const string UserRolesUpdated = "UserRolesUpdated";
    public const string UserBlocked = "UserBlocked";
    public const string UserUnblocked = "UserUnblocked";
    public const string TokenExchanged = "TokenExchanged";

    public static readonly IReadOnlyList<string> All =
    [
        LoginSucceeded,
        UserCreated,
        UserRolesUpdated,
        UserBlocked,
        UserUnblocked,
        TokenExchanged
    ];
}
