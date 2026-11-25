using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class GiftCardRevenueEntryConfiguration : IEntityTypeConfiguration<GiftCardRevenueEntry>
{
    public void Configure(EntityTypeBuilder<GiftCardRevenueEntry> builder)
    {
        builder.ToTable(
            "GiftCardRevenueEntries",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_GiftCardRevenueEntries_EntryType",
                    "\"EntryType\" IN ('Recognized','Reversed')"
                );
                t.HasCheckConstraint(
                    "CK_GiftCardRevenueEntries_Signs",
                    "(\"EntryType\" = 'Recognized' AND \"Amount\" > 0 AND \"NetAmount\" > 0 AND \"TaxAmount\" >= 0 AND \"RecognizedEntryId\" IS NULL) "
                        + "OR (\"EntryType\" = 'Reversed' AND \"Amount\" < 0 AND \"NetAmount\" <= 0 AND \"TaxAmount\" <= 0 AND \"RecognizedEntryId\" IS NOT NULL)"
                );
                t.HasCheckConstraint(
                    "CK_GiftCardRevenueEntries_Total",
                    "\"Amount\" = \"NetAmount\" + \"TaxAmount\""
                );
            }
        );
        builder.HasKey(x => x.GiftCardRevenueEntryId);
        builder
            .Property(x => x.EntryType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.NetAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxRatePercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.BusinessDate).HasColumnType("date");
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.GiftCard)
            .WithMany()
            .HasForeignKey(x => x.GiftCardId)
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
            .HasOne(x => x.RecognizedEntry)
            .WithMany()
            .HasForeignKey(x => x.RecognizedEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.PaymentTransactionId)
            .IsUnique()
            .HasDatabaseName("UX_GiftCardRevenueEntries_Transaction");
        builder
            .HasIndex(x => new { x.BusinessDate, x.EntryType })
            .HasDatabaseName("IX_GiftCardRevenueEntries_Date");
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_GiftCardRevenueEntries_Order");
        builder
            .HasIndex(x => x.GiftCardId)
            .HasDatabaseName("IX_GiftCardRevenueEntries_Card");
        builder
            .HasIndex(x => x.RecognizedEntryId)
            .HasFilter("\"RecognizedEntryId\" IS NOT NULL")
            .HasDatabaseName("IX_GiftCardRevenueEntries_Recognized");
    }
}
