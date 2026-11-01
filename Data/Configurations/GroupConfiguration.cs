using IdentityService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Data.Configurations;

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");
        builder.HasKey(group => group.Id);
        builder.Property(group => group.Name).IsRequired().HasMaxLength(Group.NameMaxLength);
        builder.HasIndex(group => group.Name).IsUnique();

        builder.HasMany(group => group.InviteCodes)
            .WithOne(code => code.Group)
            .HasForeignKey(code => code.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(group => group.Users)
            .WithOne(user => user.Group)
            .HasForeignKey(user => user.GroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
