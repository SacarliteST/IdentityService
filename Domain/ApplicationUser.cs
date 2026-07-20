using Microsoft.AspNetCore.Identity;

namespace IdentityService.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset? BlockedAt { get; set; }

    public string? BlockReason { get; set; }
}
