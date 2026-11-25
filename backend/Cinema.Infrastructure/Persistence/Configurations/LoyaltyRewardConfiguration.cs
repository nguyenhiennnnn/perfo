using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class LoyaltyRewardConfiguration : IEntityTypeConfiguration<LoyaltyReward>
{
    public void Configure(EntityTypeBuilder<LoyaltyReward> builder)
    {
        builder.ToTable(
            "LoyaltyRewards",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_LoyaltyRewards_Type",
                    "\"Type\" IN ('FreeTicket','FreeCombo','Discount')"
                );
                t.HasCheckConstraint("CK_LoyaltyRewards_PointsCost", "\"PointsCost\" > 0");
                t.HasCheckConstraint(
                    "CK_LoyaltyRewards_RemainingQuantity",
                    "\"RemainingQuantity\" >= 0"
                );
                t.HasCheckConstraint(
                    "CK_LoyaltyRewards_CodeValidityDays",
                    "\"CodeValidityDays\" > 0"
                );
                t.HasCheckConstraint(
                    "CK_LoyaltyRewards_ValidRange",
                    "\"ValidFrom\" IS NULL OR \"ValidTo\" IS NULL OR \"ValidTo\" > \"ValidFrom\""
                );
                t.HasCheckConstraint(
                    "CK_LoyaltyRewards_Discount",
                    "(\"Type\" = 'Discount' AND \"DiscountType\" IS NOT NULL AND \"DiscountValue\" > 0) "
                        + "OR (\"Type\" <> 'Discount' AND \"DiscountType\" IS NULL "
                        + "AND \"DiscountValue\" IS NULL AND \"MaxDiscountAmount\" IS NULL)"
                );
                t.HasCheckConstraint(
                    "CK_LoyaltyRewards_MaxDiscount",
                    "(\"Type\" = 'FreeTicket' AND \"MaxDiscountAmount\" > 0) "
                        + "OR (\"Type\" = 'FreeCombo' AND \"MaxDiscountAmount\" IS NULL) "
                        + "OR \"Type\" = 'Discount'"
                );
                t.HasCheckConstraint(
                    "CK_LoyaltyRewards_Combo",
                    "(\"Type\" = 'FreeCombo') = (\"ComboId\" IS NOT NULL)"
                );
            }
        );
        builder.HasKey(x => x.RewardId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.ComboName).IsRequired();
        builder
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ValidTo).HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue);
        builder.Property(x => x.DiscountValue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MaxDiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder
            .HasOne(x => x.Combo)
            .WithMany()
            .HasForeignKey(x => x.ComboId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => new { x.IsActive, x.PointsCost })
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("IX_LoyaltyRewards_Active");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
