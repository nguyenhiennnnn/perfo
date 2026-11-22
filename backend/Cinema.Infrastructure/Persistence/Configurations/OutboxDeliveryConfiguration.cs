using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class OutboxDeliveryConfiguration : IEntityTypeConfiguration<OutboxDelivery>
{
    public void Configure(EntityTypeBuilder<OutboxDelivery> builder)
    {
        builder.ToTable(
            "OutboxDeliveries",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_OutboxDeliveries_Status",
                    "\"Status\" IN ('Pending','Processing','Processed','Failed')"
                );
                t.HasCheckConstraint("CK_OutboxDeliveries_Attempts", "\"Attempts\" >= 0");
            }
        );
        builder.HasKey(x => x.OutboxDeliveryId);
        builder.Property(x => x.HandlerName).HasMaxLength(100).IsRequired();
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(OutboxDeliveryStatus.Pending);
        builder.Property(x => x.Attempts).HasDefaultValue(0);
        builder.Property(x => x.LastError).HasMaxLength(FieldLengths.ErrorMessage);
        builder.Property(x => x.NextAttemptAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.LockedUntil).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ProcessedAt).HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(x => x.Message)
            .WithMany(m => m.Deliveries)
            .HasForeignKey(x => x.OutboxMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new { x.OutboxMessageId, x.HandlerName })
            .IsUnique()
            .HasDatabaseName("UX_OutboxDeliveries_Message_Handler");
        builder
            .HasIndex(x => new { x.Status, x.NextAttemptAt })
            .HasFilter("\"Status\" IN ('Pending','Processing')")
            .HasDatabaseName("IX_OutboxDeliveries_Due");
    }
}
