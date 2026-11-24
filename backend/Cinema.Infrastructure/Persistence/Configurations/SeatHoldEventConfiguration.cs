using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class SeatHoldEventConfiguration : IEntityTypeConfiguration<SeatHoldEvent>
{
    public void Configure(EntityTypeBuilder<SeatHoldEvent> builder)
    {
        builder.ToTable(
            "SeatHoldEvents",
            t =>
                t.HasCheckConstraint(
                    "CK_SeatHoldEvents_Type",
                    "\"Type\" IN ('Held','Released','Expired')"
                )
        );
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.BusinessDate).HasColumnType("date");
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(FieldLengths.EnumValue);
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp without time zone");

        builder
            .HasIndex(x => new { x.RoomId, x.BusinessDate })
            .HasDatabaseName("IX_SeatHoldEvents_Room_Date");
        builder
            .HasIndex(x => new { x.BusinessDate, x.MovieId })
            .HasDatabaseName("IX_SeatHoldEvents_Date_Movie");
    }
}
