using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable(
            "TicketTypes",
            t =>
                t.HasCheckConstraint(
                    "CK_TicketTypes_PriceMultiplier",
                    "\"PriceMultiplier\" > 0"
                )
        );
        builder.HasKey(x => x.TicketTypeId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.Name).IsRequired();
        builder
            .HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("UX_TicketTypes_Name");
        builder
            .Property(x => x.PriceMultiplier)
            .HasColumnType("decimal(6,4)")
            .HasDefaultValue(1m);
        builder.Property(x => x.RequiresCardVerification).HasDefaultValue(false);
        builder
            .Property(x => x.AllowedAgeRatingIds)
            .HasColumnType("integer[]")
            .HasDefaultValueSql("'{}'");
        builder
            .Property(x => x.AllowedDaysOfWeek)
            .HasColumnType("integer[]")
            .HasDefaultValueSql("'{}'");
        builder.Property(x => x.AllowedFromTime).HasColumnType("time");
        builder.Property(x => x.AllowedToTime).HasColumnType("time");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
