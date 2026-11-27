using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CustomerSegmentConfiguration : IEntityTypeConfiguration<CustomerSegment>
{
    public void Configure(EntityTypeBuilder<CustomerSegment> builder)
    {
        builder.ToTable("CustomerSegments");
        builder.HasKey(x => x.SegmentId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.CustomerSegmentName).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(FieldLengths.Description);
        builder.Property(x => x.ConditionJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");

        builder
            .HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("UX_CustomerSegments_Name");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
