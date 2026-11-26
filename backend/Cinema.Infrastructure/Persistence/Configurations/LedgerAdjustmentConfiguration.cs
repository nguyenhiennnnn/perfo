using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class LedgerAdjustmentConfiguration : IEntityTypeConfiguration<LedgerAdjustment>
{
    public void Configure(EntityTypeBuilder<LedgerAdjustment> builder)
    {
        builder.ToTable(
            "LedgerAdjustments",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_LedgerAdjustments_Type",
                    "\"Type\" IN ('Refund','Cancellation','ShowtimeChange','Manual','RevenueRecognition')"
                );
                t.HasCheckConstraint(
                    "CK_LedgerAdjustments_Dates",
                    "\"PostingDate\" > \"OriginalDate\""
                );
                t.HasCheckConstraint(
                    "CK_LedgerAdjustments_NotEmpty",
                    "\"TicketRevenueDelta\" <> 0 OR \"ComboRevenueDelta\" <> 0 OR \"GiftCardSoldDelta\" <> 0 "
                        + "OR \"DiscountDelta\" <> 0 OR \"RefundAmountDelta\" <> 0 OR \"CancelledOrdersDelta\" <> 0"
                );
            }
        );
        builder.HasKey(x => x.LedgerAdjustmentId);
        builder.Property(x => x.PostingDate).HasColumnType("date");
        builder.Property(x => x.OriginalDate).HasColumnType("date");
        builder
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue);
        builder.Property(x => x.TicketRevenueDelta).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ComboRevenueDelta).HasColumnType("decimal(18,2)");
        builder.Property(x => x.GiftCardSoldDelta).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DiscountDelta).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RefundAmountDelta).HasColumnType("decimal(18,2)");
        builder
            .Property(x => x.Reason)
            .HasMaxLength(FieldLengths.LedgerAdjustmentReason)
            .IsRequired();
        builder.Property(x => x.SourceKey).HasMaxLength(FieldLengths.LedgerAdjustmentSourceKey);
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.PaymentTransaction)
            .WithMany()
            .HasForeignKey(x => x.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.SourceKey, x.OriginalDate })
            .IsUnique()
            .HasFilter("\"SourceKey\" IS NOT NULL")
            .HasDatabaseName("UX_LedgerAdjustments_Source_OriginalDate");
        builder.HasIndex(x => x.PostingDate).HasDatabaseName("IX_LedgerAdjustments_PostingDate");
        builder.HasIndex(x => x.OriginalDate).HasDatabaseName("IX_LedgerAdjustments_OriginalDate");
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_LedgerAdjustments_OrderId");
    }
}
