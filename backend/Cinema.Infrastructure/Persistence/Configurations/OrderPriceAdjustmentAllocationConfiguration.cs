using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class OrderPriceAdjustmentAllocationConfiguration
    : IEntityTypeConfiguration<OrderPriceAdjustmentAllocation>
{
    public void Configure(EntityTypeBuilder<OrderPriceAdjustmentAllocation> builder)
    {
        builder.ToTable(
            "OrderPriceAdjustmentAllocations",
            t => t.HasCheckConstraint("CK_OrderPriceAdjustmentAllocations_Amount", "\"Amount\" >= 0")
        );
        builder.HasKey(x => x.AllocationId);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");

        builder
            .HasOne(x => x.Adjustment)
            .WithMany(a => a.Allocations)
            .HasForeignKey(x => x.AdjustmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.OrderLine)
            .WithMany(l => l.AdjustmentAllocations)
            .HasForeignKey(x => x.OrderLineId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.OrderLineId).HasDatabaseName("IX_OrderPriceAdjustmentAllocations_Line");

        builder
            .HasIndex(x => new { x.AdjustmentId, x.OrderLineId })
            .IsUnique()
            .HasDatabaseName("UQ_OrderPriceAdjustmentAllocations_Adjustment_Line");
    }
}
