using Microsoft.AspNetCore.Identity;

namespace IdentityService.Domain;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
}
