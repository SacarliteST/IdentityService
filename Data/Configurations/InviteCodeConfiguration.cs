using IdentityService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Data.Configurations;

internal sealed class InviteCodeConfiguration : IEntityTypeConfiguration<InviteCode>
{
    public void Configure(EntityTypeBuilder<InviteCode> builder)
    {
        builder.ToTable("InviteCodes");
        builder.HasKey(code => code.Id);
        builder.Property(code => code.Code).IsRequired().HasMaxLength(64);
        builder.Property(code => code.Note).HasMaxLength(500);
        builder.HasIndex(code => code.Code).IsUnique();
    }
}
