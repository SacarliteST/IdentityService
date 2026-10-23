using System.Security.Cryptography;
using System.Security.Claims;
using FluentValidation;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Common;
using IdentityService.Web.Features.Auth;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Web.Features.Users;

internal sealed record ListInviteCodesQuery : IRequest<Result<IReadOnlyList<InviteCodeDto>>>;

internal sealed record CreateInviteCodeCommand(Guid ActorUserId, string? Code, string? Note, DateTimeOffset? ExpiresAt, int? MaxUses)
    : IRequest<Result<InviteCodeDto>>;

internal sealed record DeactivateInviteCodeCommand(Guid ActorUserId, Guid Id) : IRequest<Result>;

internal static class InviteCodeMapping
{
    public static InviteCodeDto ToDto(this InviteCode code) => new(
        code.Id, code.Code, code.Note, code.CreatedAt, code.ExpiresAt, code.MaxUses, code.UsedCount, code.IsActive,
        code.CreatedByUserId);
}

internal sealed class ListInviteCodesHandler(AppDbContext db)
    : IRequestHandler<ListInviteCodesQuery, Result<IReadOnlyList<InviteCodeDto>>>
{
    public async Task<Result<IReadOnlyList<InviteCodeDto>>> Handle(ListInviteCodesQuery query, CancellationToken ct)
    {
        var codes = await db.InviteCodes.AsNoTracking().OrderByDescending(code => code.CreatedAt).ToListAsync(ct);
        IReadOnlyList<InviteCodeDto> dtos = codes.Select(code => code.ToDto()).ToList();
        return Result<IReadOnlyList<InviteCodeDto>>.Success(dtos);
    }
}

internal sealed class CreateInviteCodeValidator : AbstractValidator<CreateInviteCodeRequest>
{
    public CreateInviteCodeValidator()
    {
        RuleFor(request => request.Code)
            .Must(code => code is null || code.Trim().Length is >= 4 and <= 32)
            .WithMessage("Код должен содержать от 4 до 32 символов.")
            .Must(code => code is null || code.Trim().All(symbol => Char.IsLetterOrDigit(symbol) || symbol is '-' or '_'))
            .WithMessage("Код может содержать только буквы, цифры, дефис и подчёркивание.");
        RuleFor(request => request.Note)
            .MaximumLength(500)
            .WithMessage("Заметка не должна превышать 500 символов.");
        RuleFor(request => request.MaxUses)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Лимит использований должен быть не меньше 1.")
            .When(request => request.MaxUses.HasValue);
        RuleFor(request => request.ExpiresAt)
            .Must(expires => expires is null || expires > DateTimeOffset.UtcNow)
            .WithMessage("Срок действия должен быть в будущем.");
    }
}

internal sealed class CreateInviteCodeHandler(AppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<CreateInviteCodeCommand, Result<InviteCodeDto>>
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<Result<InviteCodeDto>> Handle(CreateInviteCodeCommand command, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var text = String.IsNullOrWhiteSpace(command.Code) ? Generate() : command.Code;
        var normalized = InviteCode.Normalize(text);
        if (await db.InviteCodes.AnyAsync(code => code.Code == normalized, ct))
        {
            return Result<InviteCodeDto>.Fail(AuthErrors.InviteCodeTaken());
        }

        var code = InviteCode.Create(normalized, command.Note, command.ActorUserId, now, command.ExpiresAt, command.MaxUses);
        db.InviteCodes.Add(code);
        db.AuditEvents.Add(AuditEvent.Create(
            command.ActorUserId,
            null,
            AuditEventTypes.InviteCodeCreated,
            $"Создан код приглашения {code.Code}" +
            (code.MaxUses is null ? String.Empty : $", лимит {code.MaxUses}") +
            (code.ExpiresAt is null ? String.Empty : $", до {code.ExpiresAt:dd.MM.yyyy HH:mm} UTC") + ".",
            now));
        await db.SaveChangesAsync(ct);
        return code.ToDto();
    }

    private static string Generate()
    {
        Span<char> symbols = stackalloc char[9];
        for (var index = 0; index < symbols.Length; index++)
        {
            symbols[index] = index == 4 ? '-' : Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(symbols);
    }
}

internal sealed class DeactivateInviteCodeHandler(AppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<DeactivateInviteCodeCommand, Result>
{
    public async Task<Result> Handle(DeactivateInviteCodeCommand command, CancellationToken ct)
    {
        var code = await db.InviteCodes.FirstOrDefaultAsync(item => item.Id == command.Id, ct);
        if (code is null)
        {
            return Result.Fail(AuthErrors.InviteCodeNotFound(command.Id));
        }

        if (!code.IsActive)
        {
            return Result.Success();
        }

        code.Deactivate();
        db.AuditEvents.Add(AuditEvent.Create(
            command.ActorUserId,
            null,
            AuditEventTypes.InviteCodeDeactivated,
            $"Код приглашения {code.Code} выключен (использован {code.UsedCount} раз).",
            timeProvider.GetUtcNow()));
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal sealed class InviteCodeEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.InviteCodes.List, async (ISender sender, CancellationToken ct) =>
                (await sender.Send<ListInviteCodesQuery, Result<IReadOnlyList<InviteCodeDto>>>(new ListInviteCodesQuery(), ct)).ToOk())
            .WithName("ListInviteCodes")
            .WithTags("Коды приглашения")
            .WithSummary("Список кодов приглашения")
            .Produces<IReadOnlyList<InviteCodeDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        app.MapPost(ApiRoutes.InviteCodes.List, async (
                CreateInviteCodeRequest request,
                ClaimsPrincipal principal,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send<CreateInviteCodeCommand, Result<InviteCodeDto>>(
                    new CreateInviteCodeCommand(
                        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!),
                        request.Code, request.Note, request.ExpiresAt, request.MaxUses),
                    ct);
                return result.ToCreated(code => ApiRoutes.InviteCodes.List + "/" + code.Id);
            })
            .WithName("CreateInviteCode")
            .WithTags("Коды приглашения")
            .WithSummary("Создание кода приглашения")
            .WithDescription(
                "Регистрация с действующим кодом сразу одобряется. Пустой код — случайный. Можно задать срок и лимит использований.")
            .Produces<InviteCodeDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<CreateInviteCodeRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        app.MapPost(ApiRoutes.InviteCodes.Deactivate, async (
                Guid id,
                ClaimsPrincipal principal,
                ISender sender,
                CancellationToken ct) =>
                (await sender.Send<DeactivateInviteCodeCommand, Result>(
                    new DeactivateInviteCodeCommand(Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!), id), ct))
                .ToNoContent())
            .WithName("DeactivateInviteCode")
            .WithTags("Коды приглашения")
            .WithSummary("Выключение кода приглашения")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }
}
