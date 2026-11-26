using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class BundleDailyUsageConfiguration : IEntityTypeConfiguration<BundleDailyUsage>
{
    public void Configure(EntityTypeBuilder<BundleDailyUsage> builder)
    {
        builder.ToTable(
            "BundleDailyUsages",
            t => t.HasCheckConstraint("CK_BundleDailyUsages_Used", "\"UsedCount\" >= 0")
        );
        builder.HasKey(x => new { x.BundleId, x.BusinessDate });
        builder.Property(x => x.BusinessDate).HasColumnType("date");
        builder.Property(x => x.UsedCount).HasDefaultValue(0);

        builder
            .HasOne(x => x.Bundle)
            .WithMany()
            .HasForeignKey(x => x.BundleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.Bundle.IsDeleted);
    }
}
