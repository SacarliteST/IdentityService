namespace IdentityService.Contracts;

public static class AuditEventTypes
{
    public const string LoginSucceeded = "LoginSucceeded";
    public const string UserCreated = "UserCreated";
    public const string UserRolesUpdated = "UserRolesUpdated";
    public const string UserBlocked = "UserBlocked";
    public const string UserUnblocked = "UserUnblocked";
    public const string UserPasswordReset = "UserPasswordReset";
    public const string UserProfileUpdated = "UserProfileUpdated";
    public const string UserRegistered = "UserRegistered";
    public const string UserApproved = "UserApproved";
    public const string UserRejected = "UserRejected";
    public const string UserProfileSelfUpdated = "UserProfileSelfUpdated";
    public const string UserPasswordChanged = "UserPasswordChanged";
    public const string InviteCodeCreated = "InviteCodeCreated";
    public const string InviteCodeDeactivated = "InviteCodeDeactivated";
    public const string GroupCreated = "GroupCreated";
    public const string GroupRenamed = "GroupRenamed";
    public const string GroupDeleted = "GroupDeleted";
    public const string TokenExchanged = "TokenExchanged";

    public static readonly IReadOnlyList<string> All =
    [
        LoginSucceeded,
        UserCreated,
        UserRolesUpdated,
        UserBlocked,
        UserUnblocked,
        UserPasswordReset,
        UserProfileUpdated,
        UserRegistered,
        UserApproved,
        UserRejected,
        UserProfileSelfUpdated,
        UserPasswordChanged,
        InviteCodeCreated,
        InviteCodeDeactivated,
        GroupCreated,
        GroupRenamed,
        GroupDeleted,
        TokenExchanged
    ];
}
