using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable(
            "OrderLines",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_OrderLines_LineType",
                    "\"LineType\" IN ('Ticket','Combo','GiftCard','Fee')"
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_Shape",
                    "(\"LineType\" = 'Ticket' AND \"ShowtimeId\" IS NOT NULL "
                        + "AND \"ShowtimeSeatId\" IS NOT NULL AND \"ComboId\" IS NULL "
                        + "AND \"Quantity\" = 1) OR "
                        + "(\"LineType\" = 'Combo' AND \"ComboId\" IS NOT NULL "
                        + "AND \"ShowtimeSeatId\" IS NULL AND \"TicketTypeId\" IS NULL) OR "
                        + "(\"LineType\" IN ('GiftCard','Fee') AND \"ComboId\" IS NULL "
                        + "AND \"ShowtimeId\" IS NULL AND \"ShowtimeSeatId\" IS NULL "
                        + "AND \"TicketTypeId\" IS NULL)"
                );
                t.HasCheckConstraint("CK_OrderLines_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint(
                    "CK_OrderLines_QuantityFulfilled",
                    "\"QuantityFulfilled\" >= 0 AND \"QuantityFulfilled\" <= \"Quantity\""
                );
                t.HasCheckConstraint("CK_OrderLines_UnitCost", "\"UnitCost\" >= 0");
                t.HasCheckConstraint("CK_OrderLines_UnitListPrice", "\"UnitListPrice\" >= 0");
                t.HasCheckConstraint(
                    "CK_OrderLines_LockedUnitPrice",
                    "\"LockedUnitPrice\" >= 0"
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_PaidAmount",
                    "\"PaidAmount\" >= 0 AND \"PaidAmount\" <= \"LockedUnitPrice\" * \"Quantity\""
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_RefundedAmount",
                    "\"RefundedAmount\" >= 0 AND \"RefundedAmount\" <= \"PaidAmount\""
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_RefundedQuantity",
                    "\"RefundedQuantity\" >= 0 AND \"RefundedQuantity\" <= \"Quantity\""
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_CheckIn",
                    "(NOT \"IsCheckedIn\") OR \"LineType\" = 'Ticket'"
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_CardPresentation",
                    "(NOT \"RequiresCardPresentation\") OR \"LineType\" = 'Ticket'"
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_CardCheckStatus",
                    "\"CardCheckStatus\" IN ('NotRequired','Pending','NotPresented','Supplemented')"
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_CardCheck",
                    "\"CardCheckStatus\" = 'NotRequired' "
                        + "OR (\"LineType\" = 'Ticket' AND \"RequiresCardPresentation\")"
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_CardCheckRecord",
                    "((\"CardCheckStatus\" IN ('NotPresented','Supplemented')) "
                        + "= (\"CardCheckRecordedAt\" IS NOT NULL)) "
                        + "AND ((\"CardCheckRecordedAt\" IS NULL) "
                        + "= (\"CardCheckRecordedByUserId\" IS NULL))"
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_GiftCardData",
                    "\"LineType\" = 'GiftCard' OR (\"GiftCardId\" IS NULL "
                        + "AND \"GiftRecipientEmail\" IS NULL AND \"GiftRecipientName\" IS NULL "
                        + "AND \"GiftMessage\" IS NULL AND \"GiftSendAt\" IS NULL)"
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_SupplementedLine",
                    "\"SupplementedLineId\" IS NULL OR \"LineType\" = 'Fee'"
                );
                t.HasCheckConstraint(
                    "CK_OrderLines_ExchangeFromLine",
                    "\"ExchangeFromLineId\" IS NULL OR \"LineType\" = 'Ticket'"
                );
            }
        );
        builder.HasKey(x => x.OrderLineId);
        builder
            .Property(x => x.LineType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.Description).HasMaxLength(FieldLengths.Description);
        builder.Property(x => x.Quantity).HasDefaultValue(1);
        builder.Property(x => x.QuantityFulfilled).HasDefaultValue(0);
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(x => x.UnitListPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.LockedUnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaidAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RefundedAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.RefundedQuantity).HasDefaultValue(0);
        builder.Property(x => x.QrCode).HasMaxLength(255);
        builder.Property(x => x.RequiresCardPresentation).HasDefaultValue(false);
        builder
            .Property(x => x.CardCheckStatus)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(CardCheckStatus.NotRequired)
            .IsRequired();
        builder
            .Property(x => x.CardCheckRecordedAt)
            .HasColumnType("timestamp without time zone");
        builder.Property(x => x.IsCheckedIn).HasDefaultValue(false);
        builder.Property(x => x.CheckedInAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ReminderSentAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.GiftRecipientEmail).HasMaxLength(FieldLengths.Email);
        builder.Property(x => x.GiftRecipientName).HasMaxLength(FieldLengths.PersonName);
        builder.Property(x => x.GiftMessage).HasMaxLength(FieldLengths.GiftCardMessage);
        builder.Property(x => x.GiftSendAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Order)
            .WithMany(o => o.Lines)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_OrderLines_Order");

        builder
            .HasOne(x => x.Showtime)
            .WithMany(s => s.OrderLines)
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.ShowtimeId)
            .HasFilter("\"ShowtimeId\" IS NOT NULL")
            .HasDatabaseName("IX_OrderLines_Showtime");

        builder
            .HasOne(x => x.ShowtimeSeat)
            .WithMany(ss => ss.OrderLines)
            .HasForeignKey(x => x.ShowtimeSeatId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.ShowtimeSeatId)
            .HasFilter("\"ShowtimeSeatId\" IS NOT NULL")
            .HasDatabaseName("IX_OrderLines_ShowtimeSeat");

        builder
            .HasOne(x => x.Combo)
            .WithMany(c => c.OrderLines)
            .HasForeignKey(x => x.ComboId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.ComboId)
            .HasFilter("\"ComboId\" IS NOT NULL")
            .HasDatabaseName("IX_OrderLines_Combo");

        builder
            .HasOne(x => x.GiftCard)
            .WithMany()
            .HasForeignKey(x => x.GiftCardId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.GiftCardId)
            .IsUnique()
            .HasFilter("\"GiftCardId\" IS NOT NULL")
            .HasDatabaseName("UX_OrderLines_GiftCard");

        builder
            .HasOne(x => x.TicketType)
            .WithMany()
            .HasForeignKey(x => x.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.TicketTypeId)
            .HasFilter("\"TicketTypeId\" IS NOT NULL")
            .HasDatabaseName("IX_OrderLines_TicketType");

        builder
            .HasOne(x => x.SupplementedLine)
            .WithMany()
            .HasForeignKey(x => x.SupplementedLineId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.SupplementedLineId)
            .HasFilter("\"SupplementedLineId\" IS NOT NULL")
            .HasDatabaseName("IX_OrderLines_SupplementedLine");

        builder
            .HasOne(x => x.ExchangeFromLine)
            .WithMany()
            .HasForeignKey(x => x.ExchangeFromLineId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.ExchangeFromLineId)
            .HasFilter("\"ExchangeFromLineId\" IS NOT NULL")
            .HasDatabaseName("IX_OrderLines_ExchangeFromLine");

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CardCheckRecordedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.QrCode)
            .IsUnique()
            .HasFilter("\"QrCode\" IS NOT NULL")
            .HasDatabaseName("UX_OrderLines_QrCode");
        builder
            .HasIndex(x => x.ShowtimeId)
            .HasFilter(
                "\"LineType\" = 'Ticket' AND \"ReminderSentAt\" IS NULL AND \"QrCode\" IS NOT NULL"
            )
            .HasDatabaseName("IX_OrderLines_PendingReminder");
    }
}
