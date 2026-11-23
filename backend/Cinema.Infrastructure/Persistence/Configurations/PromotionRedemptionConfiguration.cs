using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class PromotionRedemptionConfiguration : IEntityTypeConfiguration<PromotionRedemption>
{
    public void Configure(EntityTypeBuilder<PromotionRedemption> builder)
    {
        builder.ToTable(
            "PromotionRedemptions",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_PromotionRedemptions_Status",
                    "\"Status\" IN ('Reserved','Redeemed','Released')"
                );
                t.HasCheckConstraint(
                    "CK_PromotionRedemptions_DiscountAmount",
                    "\"DiscountAmount\" >= 0"
                );
            }
        );
        builder.HasKey(x => x.PromotionRedemptionId);
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReservedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RedeemedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ReleasedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Promotion)
            .WithMany(p => p.Redemptions)
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.PromotionId, x.OrderId })
            .IsUnique()
            .HasFilter("\"Status\" <> 'Released'")
            .HasDatabaseName("UX_PromotionRedemptions_Promotion_Order_Active");
        builder
            .HasIndex(x => new { x.PromotionId, x.Status })
            .HasDatabaseName("IX_PromotionRedemptions_Promotion_Status");
        builder
            .HasIndex(x => new { x.UserId, x.PromotionId })
            .HasFilter("\"UserId\" IS NOT NULL")
            .HasDatabaseName("IX_PromotionRedemptions_User_Promotion");
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_PromotionRedemptions_Order");

        builder.HasQueryFilter(x => !x.Promotion.IsDeleted);
    }
}
