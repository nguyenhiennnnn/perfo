using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class RoomQualityAlertConfiguration : IEntityTypeConfiguration<RoomQualityAlert>
{
    public void Configure(EntityTypeBuilder<RoomQualityAlert> builder)
    {
        builder.ToTable("RoomQualityAlerts");
        builder.HasKey(x => x.RoomQualityAlertId);
        builder.Property(x => x.AverageScore).HasColumnType("decimal(4,2)");
        builder.Property(x => x.Threshold).HasColumnType("decimal(4,2)");
        builder.Property(x => x.DetectedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.NotifiedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ResolvedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Room)
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.RoomId)
            .IsUnique()
            .HasFilter("\"ResolvedAt\" IS NULL")
            .HasDatabaseName("UQ_RoomQualityAlerts_Room_Active");
        builder
            .HasIndex(x => new { x.RoomId, x.DetectedAt })
            .HasDatabaseName("IX_RoomQualityAlerts_Room_DetectedAt");
    }
}
