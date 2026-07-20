using IdentityService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Data.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.HasKey(auditEvent => auditEvent.Id);

        builder.Property(auditEvent => auditEvent.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(auditEvent => auditEvent.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(auditEvent => auditEvent.CreatedAt);
        builder.HasIndex(auditEvent => auditEvent.ActorUserId);
        builder.HasIndex(auditEvent => auditEvent.TargetUserId);
        builder.HasIndex(auditEvent => auditEvent.EventType);
    }
}
