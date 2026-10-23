using IdentityService.Common.Results;
using IdentityService.Domain;

namespace IdentityService.Web.Features.Auth;

/// <summary>Проверка допуска учётной записи по решению о заявке на регистрацию.</summary>
internal static class AccountAccess
{
    /// <summary>Ошибка, если вход запрещён состоянием заявки; <see langword="null"/> — допуск есть.</summary>
    public static Error? GetDenial(ApplicationUser user) => user.ApprovalState switch
    {
        ApprovalState.Pending => AuthErrors.AccountPendingApproval(),
        ApprovalState.Rejected => AuthErrors.AccountRejected(),
        _ => null,
    };
}
