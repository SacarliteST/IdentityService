namespace IdentityService.Contracts;

public static class ApiRoutes
{
    public const string PrefixV1 = "api/v1";

    public static class Auth
    {
        public const string Register = $"{PrefixV1}/auth/register";
        public const string Login = $"{PrefixV1}/auth/login";
        public const string Refresh = $"{PrefixV1}/auth/refresh";
        public const string Logout = $"{PrefixV1}/auth/logout";

        /// <summary>Server-to-server: обмен токена на аудиторию другого сервиса (Token Exchange).</summary>
        public const string TokenExchange = $"{PrefixV1}/auth/token/exchange";
    }

    public static class Users
    {
        public const string List = $"{PrefixV1}/users";
        public const string ById = $"{PrefixV1}/users/{{id}}";

        /// <summary>Route template — {id} заменяется на Guid пользователя.</summary>
        public const string Roles = $"{PrefixV1}/users/{{id}}/roles";
        public const string Profile = $"{PrefixV1}/users/{{id}}/profile";
        public const string Password = $"{PrefixV1}/users/{{id}}/password";
        public const string Approve = $"{PrefixV1}/users/{{id}}/approve";
        public const string Reject = $"{PrefixV1}/users/{{id}}/reject";
        public const string Block = $"{PrefixV1}/users/{{id}}/block";
        public const string Unblock = $"{PrefixV1}/users/{{id}}/unblock";
        public const string Activity = $"{PrefixV1}/users/{{id}}/activity";
    }

    /// <summary>Личный кабинет текущего пользователя.</summary>
    public static class Me
    {
        public const string Profile = $"{PrefixV1}/me";
        public const string UpdateProfile = $"{PrefixV1}/me/profile";
        public const string Password = $"{PrefixV1}/me/password";
    }

    /// <summary>Коды приглашения (администратор).</summary>
    public static class InviteCodes
    {
        public const string List = $"{PrefixV1}/invite-codes";
        public const string Deactivate = $"{PrefixV1}/invite-codes/{{id}}/deactivate";
    }

    public static class Audit
    {
        public const string List = $"{PrefixV1}/audit-events";
    }
}
