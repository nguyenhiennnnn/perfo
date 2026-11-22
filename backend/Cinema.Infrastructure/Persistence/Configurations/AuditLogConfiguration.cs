using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.AuditLogId);
        builder.Property(x => x.AuditLogId).ValueGeneratedOnAdd();
        builder
            .Property(x => x.OccurredAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        builder.Property(x => x.Action).HasMaxLength(FieldLengths.AuditAction).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(FieldLengths.AuditEntityType).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(FieldLengths.AuditEntityId).IsRequired();
        builder.Property(x => x.BeforeJson).HasColumnType("jsonb");
        builder.Property(x => x.AfterJson).HasColumnType("jsonb");

        builder.HasIndex(x => x.OccurredAt).HasDatabaseName("IX_AuditLogs_OccurredAt");
        builder
            .HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt })
            .HasDatabaseName("IX_AuditLogs_Entity");
        builder
            .HasIndex(x => new { x.ActorUserId, x.OccurredAt })
            .HasDatabaseName("IX_AuditLogs_Actor");
    }
}
