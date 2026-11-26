using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class RoomRentalRateConfiguration : IEntityTypeConfiguration<RoomRentalRate>
{
    public void Configure(EntityTypeBuilder<RoomRentalRate> builder)
    {
        builder.ToTable(
            "RoomRentalRates",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_RoomRentalRates_TimeWindow",
                    "\"EndTime\" > \"StartTime\""
                );
                t.HasCheckConstraint(
                    "CK_RoomRentalRates_Duration",
                    "\"MinDurationMinutes\" > 0 AND \"MaxDurationMinutes\" >= \"MinDurationMinutes\""
                );
                t.HasCheckConstraint(
                    "CK_RoomRentalRates_Amounts",
                    "\"BasePrice\" >= 0 AND \"PerPersonFee\" >= 0 AND \"IncludedAttendees\" >= 0"
                );
            }
        );
        builder.HasKey(x => x.RoomRentalRateId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.ComboName).IsRequired();
        builder.Property(x => x.StartTime).HasColumnType("time");
        builder.Property(x => x.EndTime).HasColumnType("time");
        builder.Property(x => x.BasePrice).HasColumnType("decimal(18,0)");
        builder.Property(x => x.PerPersonFee).HasColumnType("decimal(18,0)");
        builder.Property(x => x.IncludedAttendees).HasDefaultValue(0);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder
            .HasOne(x => x.Room)
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => new { x.RoomId, x.IsActive })
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("IX_RoomRentalRates_Room_Active");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
