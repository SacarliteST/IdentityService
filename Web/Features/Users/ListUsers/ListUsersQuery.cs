using IdentityService.Common.Cqrs;
using IdentityService.Common.Results;
using IdentityService.Contracts;

namespace IdentityService.Web.Features.Users.ListUsers;

internal sealed record ListUsersQuery(
    int Page,
    int PageSize,
    string? Search,
    string? Role,
    string? Status) : IRequest<Result<PagedResponse<UserListItemDto>>>;
