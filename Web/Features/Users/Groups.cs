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

internal sealed record ListGroupsQuery : IRequest<Result<IReadOnlyList<GroupDto>>>;

internal sealed record CreateGroupCommand(Guid ActorUserId, string Name) : IRequest<Result<GroupDto>>;

internal sealed record RenameGroupCommand(Guid ActorUserId, Guid Id, string Name) : IRequest<Result<GroupDto>>;

internal sealed record DeleteGroupCommand(Guid ActorUserId, Guid Id) : IRequest<Result>;

internal static class GroupMapping
{
    public static GroupDto ToDto(this Group group) => new(group.Id, group.Name, group.CreatedAt);
}

internal sealed class ListGroupsHandler(AppDbContext db)
    : IRequestHandler<ListGroupsQuery, Result<IReadOnlyList<GroupDto>>>
{
    public async Task<Result<IReadOnlyList<GroupDto>>> Handle(ListGroupsQuery query, CancellationToken ct)
    {
        var groups = await db.Groups.AsNoTracking().OrderBy(group => group.Name).ToListAsync(ct);
        IReadOnlyList<GroupDto> dtos = groups.Select(group => group.ToDto()).ToList();
        return Result<IReadOnlyList<GroupDto>>.Success(dtos);
    }
}

internal sealed class SaveGroupRequestValidator : AbstractValidator<SaveGroupRequest>
{
    public SaveGroupRequestValidator()
    {
        RuleFor(request => request.Name)
            .Must(name => Group.NormalizeName(name) is not null)
            .WithMessage("Название группы не должно быть пустым.")
            .Must(name => Group.NormalizeName(name)!.Length <= Group.NameMaxLength)
            .WithMessage($"Название группы не должно превышать {Group.NameMaxLength} символов.");
    }
}

internal sealed class CreateGroupHandler(AppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<CreateGroupCommand, Result<GroupDto>>
{
    public async Task<Result<GroupDto>> Handle(CreateGroupCommand command, CancellationToken ct)
    {
        var name = Group.NormalizeName(command.Name)!;
        if (await db.Groups.AnyAsync(group => group.Name == name, ct))
        {
            return Result<GroupDto>.Fail(AuthErrors.GroupNameTaken());
        }

        var group = Group.Create(name, timeProvider.GetUtcNow());
        db.Groups.Add(group);
        db.AuditEvents.Add(AuditEvent.Create(
            command.ActorUserId,
            null,
            AuditEventTypes.GroupCreated,
            $"Создана группа {group.Name}.",
            group.CreatedAt));
        await db.SaveChangesAsync(ct);
        return group.ToDto();
    }
}

internal sealed class RenameGroupHandler(AppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<RenameGroupCommand, Result<GroupDto>>
{
    public async Task<Result<GroupDto>> Handle(RenameGroupCommand command, CancellationToken ct)
    {
        var group = await db.Groups.FirstOrDefaultAsync(item => item.Id == command.Id, ct);
        if (group is null)
        {
            return Result<GroupDto>.Fail(AuthErrors.GroupNotFound(command.Id));
        }

        var name = Group.NormalizeName(command.Name)!;
        if (group.Name != name)
        {
            if (await db.Groups.AnyAsync(item => item.Name == name && item.Id != group.Id, ct))
            {
                return Result<GroupDto>.Fail(AuthErrors.GroupNameTaken());
            }

            var previous = group.Name;
            group.Rename(name);
            db.AuditEvents.Add(AuditEvent.Create(
                command.ActorUserId,
                null,
                AuditEventTypes.GroupRenamed,
                $"Группа {previous} переименована в {group.Name}.",
                timeProvider.GetUtcNow()));
            await db.SaveChangesAsync(ct);
        }

        return group.ToDto();
    }
}

internal sealed class DeleteGroupHandler(AppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<DeleteGroupCommand, Result>
{
    public async Task<Result> Handle(DeleteGroupCommand command, CancellationToken ct)
    {
        var group = await db.Groups.FirstOrDefaultAsync(item => item.Id == command.Id, ct);
        if (group is null)
        {
            return Result.Fail(AuthErrors.GroupNotFound(command.Id));
        }

        var inUse = await db.Users.AnyAsync(user => user.GroupId == group.Id, ct)
            || await db.InviteCodes.AnyAsync(code => code.GroupId == group.Id, ct);
        if (inUse)
        {
            return Result.Fail(AuthErrors.GroupInUse());
        }

        db.Groups.Remove(group);
        db.AuditEvents.Add(AuditEvent.Create(
            command.ActorUserId,
            null,
            AuditEventTypes.GroupDeleted,
            $"Группа {group.Name} удалена.",
            timeProvider.GetUtcNow()));
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

internal sealed class GroupEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Groups.List, async (ISender sender, CancellationToken ct) =>
                (await sender.Send<ListGroupsQuery, Result<IReadOnlyList<GroupDto>>>(new ListGroupsQuery(), ct)).ToOk())
            .WithName("ListGroups")
            .WithTags("Группы")
            .WithSummary("Список учебных групп")
            .Produces<IReadOnlyList<GroupDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        app.MapPost(ApiRoutes.Groups.List, async (
                SaveGroupRequest request,
                ClaimsPrincipal principal,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send<CreateGroupCommand, Result<GroupDto>>(
                    new CreateGroupCommand(Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!), request.Name),
                    ct);
                return result.ToCreated(group => ApiRoutes.Groups.List + "/" + group.Id);
            })
            .WithName("CreateGroup")
            .WithTags("Группы")
            .WithSummary("Создание группы")
            .Produces<GroupDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<SaveGroupRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        app.MapPut(ApiRoutes.Groups.ById, async (
                Guid id,
                SaveGroupRequest request,
                ClaimsPrincipal principal,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send<RenameGroupCommand, Result<GroupDto>>(
                    new RenameGroupCommand(Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!), id, request.Name),
                    ct);
                return result.ToOk();
            })
            .WithName("RenameGroup")
            .WithTags("Группы")
            .WithSummary("Переименование группы")
            .WithDescription("Переименование видно в новых токенах (claim group_name) сразу, в выданных — до их истечения.")
            .Produces<GroupDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<SaveGroupRequest>>()
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        app.MapDelete(ApiRoutes.Groups.ById, async (
                Guid id,
                ClaimsPrincipal principal,
                ISender sender,
                CancellationToken ct) =>
                (await sender.Send<DeleteGroupCommand, Result>(
                    new DeleteGroupCommand(Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!), id), ct))
                .ToNoContent())
            .WithName("DeleteGroup")
            .WithTags("Группы")
            .WithSummary("Удаление группы")
            .WithDescription("Удалить можно только группу, в которую не зачислен ни один пользователь и которая не указана в коде приглашения.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }
}
