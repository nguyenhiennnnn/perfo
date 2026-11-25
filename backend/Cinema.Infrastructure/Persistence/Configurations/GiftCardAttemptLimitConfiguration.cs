using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class GiftCardAttemptLimitConfiguration : IEntityTypeConfiguration<GiftCardAttemptLimit>
{
    public void Configure(EntityTypeBuilder<GiftCardAttemptLimit> builder)
    {
        builder.ToTable("GiftCardAttemptLimit");
        builder.HasKey(x => x.Key);
        builder.Property(x => x.Key).HasMaxLength(FieldLengths.GiftCardAttemptKey);
        builder.Property(x => x.WindowStartedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.LockedUntil).HasColumnType("timestamp without time zone");
    }
}
