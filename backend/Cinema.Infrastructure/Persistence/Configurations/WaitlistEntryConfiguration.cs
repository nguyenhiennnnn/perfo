using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WaitlistEntry> builder)
    {
        builder.ToTable(
            "WaitlistEntries",
            t =>
            {
                t.HasCheckConstraint("CK_WaitlistEntries_SeatCount", "\"SeatCount\" >= 1");
                t.HasCheckConstraint("CK_WaitlistEntries_Position", "\"Position\" >= 1");
            }
        );
        builder.HasKey(x => x.WaitlistEntryId);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.NotifiedAt).HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.PriorityExpiresAt)
            .HasColumnType("timestamp without time zone");
        builder.Property(x => x.ClosedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Showtime)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.ShowtimeId, x.Position })
            .IsUnique()
            .HasDatabaseName("UQ_WaitlistEntries_Showtime_Position");
        builder
            .HasIndex(x => new { x.ShowtimeId, x.UserId })
            .IsUnique()
            .HasFilter("\"Status\" IN ('Waiting', 'Notified', 'Held')")
            .HasDatabaseName("UQ_WaitlistEntries_Showtime_User_Active");
        builder
            .HasIndex(x => new { x.ShowtimeId, x.Status, x.Position })
            .HasDatabaseName("IX_WaitlistEntries_Showtime_Status_Position");
        builder
            .HasIndex(x => x.PriorityExpiresAt)
            .HasFilter("\"Status\" = 'Notified'")
            .HasDatabaseName("IX_WaitlistEntries_PriorityExpiresAt");
        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_WaitlistEntries_User");
    }
}
