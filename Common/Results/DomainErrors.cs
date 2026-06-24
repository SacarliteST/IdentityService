using System.Runtime.CompilerServices;

namespace IdentityService.Common.Results;

public static class DomainErrors<TEntity>
{
    private static string Prefix => typeof(TEntity).Name;

    public static Error Validation(string message, [CallerMemberName] string reason = "") =>
        new($"{Prefix}.{reason}", message, ErrorType.Validation);

    public static Error Unauthorized(string message, [CallerMemberName] string reason = "") =>
        new($"{Prefix}.{reason}", message, ErrorType.Unauthorized);

    public static Error NotFound(object id, [CallerMemberName] string reason = "") =>
        new($"{Prefix}.{reason}", $"{Prefix} с id '{id}' не найден(а).", ErrorType.NotFound);

    public static Error Conflict(string message, [CallerMemberName] string reason = "") =>
        new($"{Prefix}.{reason}", message, ErrorType.Conflict);

    public static Error Failure(string message, [CallerMemberName] string reason = "") =>
        new($"{Prefix}.{reason}", message, ErrorType.Failure);
}
