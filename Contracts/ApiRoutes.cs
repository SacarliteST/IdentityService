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
    }

    public static class Users
    {
        public const string List = $"{PrefixV1}/users";
        public const string ById = $"{PrefixV1}/users/{{id}}";

        /// <summary>Route template — {id} заменяется на Guid пользователя.</summary>
        public const string Roles = $"{PrefixV1}/users/{{id}}/roles";
        public const string Block = $"{PrefixV1}/users/{{id}}/block";
        public const string Unblock = $"{PrefixV1}/users/{{id}}/unblock";
    }
}
