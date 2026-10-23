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

        builder.Property(user => user.ApprovalState).HasConversion<int>().HasDefaultValue(ApprovalState.Approved);
        builder.HasIndex(user => user.ApprovalState);
        builder.Property(user => user.FirstName).HasMaxLength(PersonName.MaxPartLength);
        builder.Property(user => user.LastName).HasMaxLength(PersonName.MaxPartLength);
        builder.Property(user => user.MiddleName).HasMaxLength(PersonName.MaxPartLength);

        builder.HasIndex(user => user.CreatedAt);
        builder.HasIndex(user => user.LastLoginAt);
        builder.HasIndex(user => user.LockoutEnd);
    }
}
