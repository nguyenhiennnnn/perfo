using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class SpecialDayConfiguration : IEntityTypeConfiguration<SpecialDay>
{
    public void Configure(EntityTypeBuilder<SpecialDay> builder)
    {
        builder.ToTable(
            "SpecialDays",
            t => t.HasCheckConstraint("CK_SpecialDays_Range", "\"EndDate\" >= \"StartDate\"")
        );
        builder.HasKey(x => x.SpecialDayId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.PersonName).IsRequired();
        builder.Property(x => x.StartDate).HasColumnType("date");
        builder.Property(x => x.EndDate).HasColumnType("date");
        builder
            .HasIndex(x => new { x.Name, x.StartDate })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("UX_SpecialDays_Name_StartDate");
        builder
            .HasIndex(x => new { x.StartDate, x.EndDate })
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("IX_SpecialDays_Range");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
