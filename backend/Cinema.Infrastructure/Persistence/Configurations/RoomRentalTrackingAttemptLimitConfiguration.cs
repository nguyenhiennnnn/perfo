using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class RoomRentalTrackingAttemptLimitConfiguration
    : IEntityTypeConfiguration<RoomRentalTrackingAttemptLimit>
{
    public void Configure(EntityTypeBuilder<RoomRentalTrackingAttemptLimit> builder)
    {
        builder.ToTable("RoomRentalTrackingAttemptLimits");
        builder.HasKey(x => x.Key);
        builder.Property(x => x.Key).HasMaxLength(FieldLengths.PublicFormAttemptKey);
        builder.Property(x => x.WindowStartedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.LockedUntil).HasColumnType("timestamp without time zone");
    }
}
