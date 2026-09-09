using IdentityService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Data.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClientId)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(c => c.ClientId)
            .IsUnique();

        builder.Property(c => c.ClientSecretHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(c => c.AllowedAudiences)
            .IsRequired()
            .HasColumnType("text[]");
    }
}
