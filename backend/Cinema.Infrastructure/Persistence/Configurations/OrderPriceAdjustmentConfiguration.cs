using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class OrderPriceAdjustmentConfiguration : IEntityTypeConfiguration<OrderPriceAdjustment>
{
    public void Configure(EntityTypeBuilder<OrderPriceAdjustment> builder)
    {
        builder.ToTable(
            "OrderPriceAdjustments",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_OrderPriceAdjustments_Type",
                    "\"Type\" IN ('TierDiscount','MembershipBenefit','Promotion','PackagePrice')"
                );
                t.HasCheckConstraint("CK_OrderPriceAdjustments_Amount", "\"Amount\" >= 0");
                t.HasCheckConstraint(
                    "CK_OrderPriceAdjustments_Reference",
                    "(\"ReferenceType\" IS NULL) = (\"ReferenceId\" IS NULL)"
                );
            }
        );
        builder.HasKey(x => x.AdjustmentId);
        builder
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReferenceType).HasMaxLength(FieldLengths.ReferenceType);

        builder
            .HasOne(x => x.Order)
            .WithMany(o => o.PriceAdjustments)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_OrderPriceAdjustments_Order");
        builder
            .HasIndex(x => new
            {
                x.Type,
                x.ReferenceType,
                x.ReferenceId,
            })
            .HasDatabaseName("IX_OrderPriceAdjustments_Reference");
    }
}
