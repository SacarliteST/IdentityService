using System.Security.Claims;
using FluentValidation;
using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;
using IdentityService.Data;
using IdentityService.Domain;
using IdentityService.Web.Common;
using IdentityService.Web.Features.Auth;
using IdentityService.Web.Features.Users;
using IdentityService.Web.Features.Users.UpdateUserProfile;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Web.Features.Me;

internal sealed record GetMyProfileQuery(Guid UserId) : IRequest<Result<MyProfileDto>>;

internal sealed class GetMyProfileHandler(
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider) : IRequestHandler<GetMyProfileQuery, Result<MyProfileDto>>
{
    public async Task<Result<MyProfileDto>> Handle(GetMyProfileQuery query, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(query.UserId.ToString());
        if (user is null)
        {
            return Result<MyProfileDto>.Fail(AuthErrors.UserNotFound(query.UserId));
        }

        var roles = (await userManager.GetRolesAsync(user)).Order().ToList();
        return new MyProfileDto(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.FirstName,
            user.LastName,
            user.MiddleName,
            roles,
            UserStatusResolver.Resolve(user.LockoutEnabled, user.LockoutEnd, timeProvider.GetUtcNow(), user.ApprovalState),
            user.CreatedAt,
            user.LastLoginAt);
    }
}

internal sealed class MyProfileEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Me.Profile, async (ClaimsPrincipal principal, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send<GetMyProfileQuery, Result<MyProfileDto>>(
                    new GetMyProfileQuery(CurrentUserId(principal)), ct);
                return result.ToOk();
            })
            .WithName("GetMyProfile")
            .WithTags("Личный кабинет")
            .WithSummary("Мои данные")
            .WithDescription("Данные текущего пользователя: ФИО, email, роли, статус.")
            .Produces<MyProfileDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

        app.MapPut(ApiRoutes.Me.UpdateProfile, async (
                UpdateUserProfileRequest request,
                ClaimsPrincipal principal,
                ISender sender,
                CancellationToken ct) =>
            {
                var userId = CurrentUserId(principal);
                var result = await sender.Send<UpdateUserProfileCommand, Result>(
                    new UpdateUserProfileCommand(
                        userId, userId, request.FirstName, request.LastName, request.MiddleName, BySelf: true),
                    ct);
                return result.ToNoContent();
            })
            .WithName("UpdateMyProfile")
            .WithTags("Личный кабинет")
            .WithSummary("Изменение своего ФИО")
            .WithDescription(
                "Меняет имя, фамилию и отчество текущего пользователя. Каждое изменение пишется в журнал аудита " +
                "(администратор видит прежнее и новое значение). Email и роли самостоятельно не меняются.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<UpdateUserProfileRequest>>()
            .RequireAuthorization();

        app.MapPut(ApiRoutes.Me.Password, async (
                ChangeMyPasswordRequest request,
                ClaimsPrincipal principal,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send<ChangeMyPasswordCommand, Result>(
                    new ChangeMyPasswordCommand(CurrentUserId(principal), request.CurrentPassword, request.NewPassword), ct);
                return result.ToNoContent();
            })
            .WithName("ChangeMyPassword")
            .WithTags("Личный кабинет")
            .WithSummary("Смена своего пароля")
            .WithDescription(
                "Требует текущий пароль. После смены все refresh-токены пользователя отзываются — нужно войти заново. " +
                "Событие пишется в журнал аудита (без паролей).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<ChangeMyPasswordRequest>>()
            .RequireRateLimiting(RateLimitingExtensions.AuthPolicy)
            .RequireAuthorization();
    }

    private static Guid CurrentUserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

internal sealed record ChangeMyPasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<Result>;

internal sealed class ChangeMyPasswordValidator : FluentValidation.AbstractValidator<ChangeMyPasswordRequest>
{
    public ChangeMyPasswordValidator()
    {
        RuleFor(request => request.CurrentPassword).NotEmpty().WithMessage("Укажите текущий пароль.");
        RuleFor(request => request.NewPassword)
            .NotEmpty()
            .WithMessage("Укажите новый пароль.")
            .MinimumLength(8)
            .WithMessage("Пароль должен содержать не менее 8 символов.")
            .Matches("[A-Z]")
            .WithMessage("Пароль должен содержать заглавную букву.")
            .Matches("[a-z]")
            .WithMessage("Пароль должен содержать строчную букву.")
            .Matches("[0-9]")
            .WithMessage("Пароль должен содержать цифру.");
    }
}

internal sealed class ChangeMyPasswordHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider) : IRequestHandler<ChangeMyPasswordCommand, Result>
{
    public async Task<Result> Handle(ChangeMyPasswordCommand command, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null)
        {
            return Result.Fail(AuthErrors.UserNotFound(command.UserId));
        }

        var now = timeProvider.GetUtcNow();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var change = await userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        if (!change.Succeeded)
        {
            return Result.Fail(change.Errors.Any(error => error.Code == "PasswordMismatch")
                ? AuthErrors.WrongCurrentPassword()
                : AuthErrors.FromIdentityErrors(change.Errors));
        }

        user.UpdatedAt = now;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            return Result.Fail(AuthErrors.FromIdentityErrors(update.Errors));
        }

        // Все прежние сессии должны перестать работать: украденный refresh-токен не переживает смену пароля.
        var activeTokens = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            db.RefreshTokens.Where(token => token.UserId == user.Id && token.RevokedAt == null && token.ExpiresAt > now),
            ct);
        foreach (var token in activeTokens)
        {
            token.Revoke(now);
        }

        db.AuditEvents.Add(AuditEvent.Create(
            user.Id,
            user.Id,
            AuditEventTypes.UserPasswordChanged,
            "Пользователь сменил пароль; активные сессии отозваны.",
            now));
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Result.Success();
    }
}
