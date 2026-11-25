using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class SeatBlockConfiguration : IEntityTypeConfiguration<SeatBlock>
{
    public void Configure(EntityTypeBuilder<SeatBlock> builder)
    {
        builder.ToTable(
            "SeatBlocks",
            t =>
                t.HasCheckConstraint(
                    "CK_SeatBlocks_Type",
                    "\"Type\" IN ('Maintenance','Broken','RoomRental','WaitlistReserve')"
                )
        );
        builder.HasKey(x => x.SeatBlockId);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(FieldLengths.EnumValue);
        builder.Property(x => x.Reason).HasMaxLength(FieldLengths.SeatBlockReason).IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(x => x.Showtime)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.ShowtimeSeat)
            .WithOne(ss => ss.SeatBlock)
            .HasForeignKey<SeatBlock>(x => x.ShowtimeSeatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.WaitlistEntry)
            .WithMany()
            .HasForeignKey(x => x.WaitlistEntryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(x => x.ShowtimeSeatId)
            .IsUnique()
            .HasDatabaseName("UQ_SeatBlocks_ShowtimeSeat");
        builder
            .HasIndex(x => new { x.ShowtimeId, x.Type })
            .HasDatabaseName("IX_SeatBlocks_Showtime_Type");
        builder
            .HasIndex(x => x.WaitlistEntryId)
            .HasFilter("\"WaitlistEntryId\" IS NOT NULL")
            .HasDatabaseName("IX_SeatBlocks_WaitlistEntry");
        builder
            .HasIndex(x => x.ExpiresAt)
            .HasFilter("\"ExpiresAt\" IS NOT NULL")
            .HasDatabaseName("IX_SeatBlocks_ExpiresAt");
    }
}
