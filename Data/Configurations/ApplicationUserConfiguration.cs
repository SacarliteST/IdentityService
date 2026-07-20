using IdentityService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Data.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(user => user.BlockReason)
            .HasMaxLength(500);

        builder.HasIndex(user => user.CreatedAt);
        builder.HasIndex(user => user.LastLoginAt);
        builder.HasIndex(user => user.LockoutEnd);
    }
}
