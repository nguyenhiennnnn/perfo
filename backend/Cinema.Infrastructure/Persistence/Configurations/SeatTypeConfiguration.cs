using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class SeatTypeConfiguration : IEntityTypeConfiguration<SeatType>
{
    public void Configure(EntityTypeBuilder<SeatType> builder)
    {
        builder.ToTable(
            "SeatTypes",
            t => t.HasCheckConstraint("CK_SeatTypes_PriceMultiplier", "\"PriceMultiplier\" > 0")
        );
        builder.HasKey(x => x.SeatTypeId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder
            .Property(x => x.PriceMultiplier)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(1.0m);
        builder.Property(x => x.IsCouple).HasDefaultValue(false);
    }
}
