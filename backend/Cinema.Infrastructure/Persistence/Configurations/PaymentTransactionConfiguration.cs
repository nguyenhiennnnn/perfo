using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable(
            "PaymentTransactions",
            t =>
            {
                t.HasCheckConstraint("CK_PaymentTransactions_Type", "\"Type\" IN ('Charge','Refund')");
                t.HasCheckConstraint(
                    "CK_PaymentTransactions_Method",
                    "\"Method\" IN ('VNPay','Momo','Cash','Stripe','GiftCard','BankTransfer','Card')"
                );
                t.HasCheckConstraint(
                    "CK_PaymentTransactions_Status",
                    "\"Status\" IN ('Pending','Success','Failed')"
                );
                t.HasCheckConstraint("CK_PaymentTransactions_Amount", "\"Amount\" > 0");
                t.HasCheckConstraint("CK_PaymentTransactions_GatewayFee", "\"GatewayFee\" >= 0");
                t.HasCheckConstraint(
                    "CK_PaymentTransactions_Original",
                    "(\"Type\" = 'Refund') = (\"OriginalTransactionId\" IS NOT NULL)"
                );
                t.HasCheckConstraint(
                    "CK_PaymentTransactions_Completed",
                    "\"Status\" <> 'Success' OR \"CompletedAt\" IS NOT NULL"
                );
                t.HasCheckConstraint(
                    "CK_PaymentTransactions_Cash",
                    "(\"AmountReceived\" IS NULL AND \"ChangeGiven\" IS NULL) OR "
                        + "(\"AmountReceived\" IS NOT NULL AND \"ChangeGiven\" IS NOT NULL "
                        + "AND \"Method\" = 'Cash' AND \"Type\" = 'Charge' "
                        + "AND \"AmountReceived\" >= \"Amount\" "
                        + "AND \"ChangeGiven\" = \"AmountReceived\" - \"Amount\")"
                );
                t.HasCheckConstraint(
                    "CK_PaymentTransactions_ManualRefund",
                    "NOT \"NeedsManualRefund\" OR \"Type\" = 'Charge'"
                );
            }
        );
        builder.HasKey(x => x.PaymentTransactionId);
        builder
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder
            .Property(x => x.Method)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.GatewayFee).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.TransactionCode).HasMaxLength(FieldLengths.TransactionCode).IsRequired();
        builder.Property(x => x.GatewayTransactionId).HasMaxLength(FieldLengths.TransactionCode);
        builder.Property(x => x.GatewayResponseCode).HasMaxLength(FieldLengths.GatewayResponseCode);
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.PaymentStatus.Pending);
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.AmountReceived).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ChangeGiven).HasColumnType("decimal(18,2)");
        builder.Property(x => x.NeedsManualRefund).HasDefaultValue(false);
        builder.Property(x => x.Note).HasMaxLength(FieldLengths.ErrorMessage);

        builder
            .HasOne(x => x.Order)
            .WithMany(o => o.PaymentTransactions)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => new { x.OrderId, x.Type, x.Status })
            .HasDatabaseName("IX_PaymentTransactions_Order");

        builder
            .HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.OriginalTransaction)
            .WithMany(t => t.Refunds)
            .HasForeignKey(x => x.OriginalTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.OriginalTransactionId)
            .HasFilter("\"OriginalTransactionId\" IS NOT NULL")
            .HasDatabaseName("IX_PaymentTransactions_Original");

        builder
            .HasOne(x => x.CounterShift)
            .WithMany()
            .HasForeignKey(x => x.CounterShiftId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.CounterShiftId)
            .HasFilter("\"CounterShiftId\" IS NOT NULL")
            .HasDatabaseName("IX_PaymentTransactions_CounterShift");

        builder
            .HasIndex(x => x.TransactionCode)
            .IsUnique()
            .HasDatabaseName("UX_PaymentTransactions_TransactionCode");
        builder
            .HasIndex(x => new { x.Method, x.GatewayTransactionId })
            .IsUnique()
            .HasFilter("\"GatewayTransactionId\" IS NOT NULL")
            .HasDatabaseName("UX_PaymentTransactions_Method_Gateway");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_PaymentTransactions_Status");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_PaymentTransactions_CreatedAt");
        builder
            .HasOne(x => x.GiftCard)
            .WithMany()
            .HasForeignKey(x => x.GiftCardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.GiftCardId)
            .HasFilter("\"GiftCardId\" IS NOT NULL")
            .HasDatabaseName("IX_PaymentTransactions_GiftCard");
        builder
            .HasIndex(x => x.NeedsManualRefund)
            .HasFilter("\"NeedsManualRefund\"")
            .HasDatabaseName("IX_PaymentTransactions_NeedsManualRefund");
    }
}
