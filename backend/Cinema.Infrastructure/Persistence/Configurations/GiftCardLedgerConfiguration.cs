using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class GiftCardLedgerConfiguration : IEntityTypeConfiguration<GiftCardLedger>
{
    public void Configure(EntityTypeBuilder<GiftCardLedger> builder)
    {
        builder.ToTable(
            "GiftCardLedger",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_GiftCardLedger_EntryType",
                    "\"EntryType\" IN ('Issue','Reserve','Release','Redeem','Refund','Expire','Cancel')"
                );
                t.HasCheckConstraint(
                    "CK_GiftCardLedger_Signs",
                    "(\"EntryType\" = 'Issue' AND \"BalanceChange\" > 0 AND \"ReservedChange\" = 0) "
                        + "OR (\"EntryType\" = 'Reserve' AND \"BalanceChange\" = 0 AND \"ReservedChange\" > 0) "
                        + "OR (\"EntryType\" = 'Release' AND \"BalanceChange\" = 0 AND \"ReservedChange\" < 0) "
                        + "OR (\"EntryType\" = 'Redeem' AND \"BalanceChange\" < 0 AND \"ReservedChange\" <= 0) "
                        + "OR (\"EntryType\" = 'Refund' AND \"BalanceChange\" > 0 AND \"ReservedChange\" = 0) "
                        + "OR (\"EntryType\" IN ('Expire','Cancel') AND \"BalanceChange\" <= 0 AND \"ReservedChange\" <= 0 "
                        + "AND (\"BalanceChange\" < 0 OR \"ReservedChange\" < 0))"
                );
                t.HasCheckConstraint(
                    "CK_GiftCardLedger_After",
                    "\"BalanceAfter\" >= 0 AND \"ReservedAfter\" >= 0 AND \"ReservedAfter\" <= \"BalanceAfter\""
                );
            }
        );
        builder.HasKey(x => x.GiftCardLedgerId);
        builder
            .Property(x => x.EntryType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.BalanceChange).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReservedChange).HasColumnType("decimal(18,2)");
        builder.Property(x => x.BalanceAfter).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReservedAfter).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.GiftCard)
            .WithMany(c => c.LedgerEntries)
            .HasForeignKey(x => x.GiftCardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.GiftCardId, x.GiftCardLedgerId })
            .HasDatabaseName("IX_GiftCardLedger_Card");
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_GiftCardLedger_Order");
    }
}
