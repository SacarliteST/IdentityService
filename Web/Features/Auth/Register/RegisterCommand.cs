using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Auth.Register;

internal sealed record RegisterCommand(
    string Email,
    string Password,
    string? DisplayName,
    string? FirstName = null,
    string? LastName = null,
    string? MiddleName = null,
    string? InviteCode = null) : IRequest<Result<RegisterResult>>;

/// <summary>Итог регистрации: токены, если доступ дан сразу, иначе <see langword="null"/> и статус заявки.</summary>
internal sealed record RegisterResult(TokenResponse? Tokens, string Status, string Email);
