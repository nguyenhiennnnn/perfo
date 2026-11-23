using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class StockLedgerConfiguration : IEntityTypeConfiguration<StockLedger>
{
    public void Configure(EntityTypeBuilder<StockLedger> builder)
    {
        builder.ToTable(
            "StockLedger",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_StockLedger_MovementType",
                    "\"MovementType\" IN ('Receipt','Sale','Return','StocktakeAdjustment','Waste')"
                );
                t.HasCheckConstraint("CK_StockLedger_QuantityChange", "\"QuantityChange\" <> 0");
                t.HasCheckConstraint("CK_StockLedger_BalanceAfter", "\"BalanceAfter\" >= 0");
            }
        );
        builder.HasKey(x => x.StockLedgerId);
        builder
            .Property(x => x.MovementType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.QuantityChange).HasColumnType("decimal(18,3)");
        builder.Property(x => x.BalanceAfter).HasColumnType("decimal(18,3)");
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReferenceType).HasMaxLength(FieldLengths.ReferenceType);
        builder.Property(x => x.Reason).HasMaxLength(FieldLengths.StockNote);
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp without time zone");
        builder
            .HasIndex(x => new
            {
                x.InventoryItemId,
                x.OccurredAt,
                x.StockLedgerId,
            })
            .HasDatabaseName("IX_StockLedger_Item_OccurredAt");
        builder
            .HasIndex(x => new { x.ReferenceType, x.ReferenceId })
            .HasDatabaseName("IX_StockLedger_Reference");

        builder
            .HasOne(x => x.InventoryItem)
            .WithMany(i => i.LedgerEntries)
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
