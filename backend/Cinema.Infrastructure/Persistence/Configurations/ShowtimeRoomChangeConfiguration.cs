using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ShowtimeRoomChangeConfiguration : IEntityTypeConfiguration<ShowtimeRoomChange>
{
    public void Configure(EntityTypeBuilder<ShowtimeRoomChange> builder)
    {
        builder.ToTable(
            "ShowtimeRoomChanges",
            t =>
            {
                t.HasCheckConstraint("CK_ShowtimeRoomChanges_Rooms", "\"FromRoomId\" <> \"ToRoomId\"");
                t.HasCheckConstraint("CK_ShowtimeRoomChanges_SeatsMoved", "\"SeatsMoved\" >= 0");
            }
        );
        builder.HasKey(x => x.ShowtimeRoomChangeId);
        builder.Property(x => x.Note).HasMaxLength(FieldLengths.ShowtimeRoomChangeNote);
        builder
            .Property(x => x.ChangedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(x => x.Showtime)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.FromRoom)
            .WithMany()
            .HasForeignKey(x => x.FromRoomId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.ToRoom)
            .WithMany()
            .HasForeignKey(x => x.ToRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ShowtimeId).HasDatabaseName("IX_ShowtimeRoomChanges_Showtime");
    }
}
