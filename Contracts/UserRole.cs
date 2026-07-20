using System.Text.Json.Serialization;

namespace IdentityService.Contracts;

/// <summary>Поддерживаемые роли пользователя.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserRole>))]
public enum UserRole
{
    Admin,
    Teacher,
    Student
}
