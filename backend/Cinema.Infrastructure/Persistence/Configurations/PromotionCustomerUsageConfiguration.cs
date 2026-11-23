using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class PromotionCustomerUsageConfiguration : IEntityTypeConfiguration<PromotionCustomerUsage>
{
    public void Configure(EntityTypeBuilder<PromotionCustomerUsage> builder)
    {
        builder.ToTable(
            "PromotionCustomerUsages",
            t =>
                t.HasCheckConstraint(
                    "CK_PromotionCustomerUsages_Consumed",
                    "\"ConsumedCount\" >= 0"
                )
        );
        builder.HasKey(x => new { x.PromotionId, x.UserId });
        builder.Property(x => x.ConsumedCount).HasDefaultValue(0);

        builder
            .HasOne(x => x.Promotion)
            .WithMany()
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_PromotionCustomerUsages_User");

        builder.HasQueryFilter(x => !x.Promotion.IsDeleted);
    }
}
