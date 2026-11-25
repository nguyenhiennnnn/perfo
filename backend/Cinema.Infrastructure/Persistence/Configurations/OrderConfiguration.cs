using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(
            "Orders",
            t =>
            {
                t.HasCheckConstraint("CK_Orders_TotalAmount", "\"TotalAmount\" >= 0");
                t.HasCheckConstraint("CK_Orders_DiscountAmount", "\"DiscountAmount\" >= 0");
                t.HasCheckConstraint("CK_Orders_FinalAmount", "\"FinalAmount\" >= 0");
                t.HasCheckConstraint(
                    "CK_Orders_Amounts",
                    "\"DiscountAmount\" <= \"TotalAmount\" AND "
                        + "\"FinalAmount\" = \"TotalAmount\" - \"DiscountAmount\""
                );
                t.HasCheckConstraint(
                    "CK_Orders_Type",
                    "\"Type\" IN ('Ticket','Combo','GiftCard','Membership','RoomRental','Supplement','Exchange')"
                );
                t.HasCheckConstraint("CK_Orders_Channel", "\"Channel\" IN ('Online','Counter')");
                t.HasCheckConstraint(
                    "CK_Orders_Status",
                    "\"Status\" IN ('Pending','Confirmed','Cancelled','Expired')"
                );
                t.HasCheckConstraint(
                    "CK_Orders_CancelReason",
                    "\"CancelReason\" IS NULL OR \"CancelReason\" IN "
                        + "('CustomerChangedMind','DuplicateOrder','PaymentError',"
                        + "'ShowtimeCancelled','RoomClosed','Other')"
                );
                t.HasCheckConstraint(
                    "CK_Orders_CancelledBy",
                    "\"CancelledBy\" IS NULL OR \"CancelledBy\" IN "
                        + "('Customer','Staff','Admin','System')"
                );
                t.HasCheckConstraint(
                    "CK_Orders_Cancellation",
                    "(\"Status\" = 'Cancelled' AND \"CancelReason\" IS NOT NULL "
                        + "AND \"CancelledBy\" IS NOT NULL AND \"CancelledAt\" IS NOT NULL) OR "
                        + "(\"Status\" <> 'Cancelled' AND \"CancelReason\" IS NULL "
                        + "AND \"CancelledBy\" IS NULL AND \"CancelledAt\" IS NULL)"
                );
                t.HasCheckConstraint(
                    "CK_Orders_Channel_Employee",
                    "(\"Channel\" = 'Counter' AND \"CreatedByEmployeeId\" IS NOT NULL) OR "
                        + "(\"Channel\" = 'Online' AND \"CreatedByEmployeeId\" IS NULL)"
                );
                t.HasCheckConstraint(
                    "CK_Orders_Customer_Identity",
                    "\"UserId\" IS NOT NULL OR \"CustomerName\" IS NOT NULL"
                );
                t.HasCheckConstraint(
                    "CK_Orders_Supplement_Original",
                    "(\"Type\" IN ('Supplement','Exchange')) = (\"OriginalOrderId\" IS NOT NULL)"
                );
            }
        );
        builder.HasKey(x => x.OrderId);
        builder.Property(x => x.OrderCode).HasMaxLength(FieldLengths.OrderCode).IsRequired();
        builder.HasIndex(x => x.OrderCode).IsUnique();
        builder
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.OrderType.Ticket);
        builder
            .Property(x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.SalesChannel.Online);
        builder.Property(x => x.CustomerName).HasMaxLength(FieldLengths.FullName);
        builder.Property(x => x.CustomerPhone).HasMaxLength(FieldLengths.Phone);
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.OrderStatus.Pending);
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.FinalAmount).HasColumnType("decimal(18,2)");
        builder
            .Property(x => x.LoyaltyReversedAmount)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);
        builder
            .Property(x => x.CancelReason)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue);
        builder
            .Property(x => x.CancelledBy)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue);
        builder.Property(x => x.CancelledAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("IX_Orders_User")
            .IsDescending(false, true);

        builder
            .HasOne(x => x.CreatedByEmployee)
            .WithMany(e => e.CreatedOrders)
            .HasForeignKey(x => x.CreatedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.CreatedByEmployeeId).HasDatabaseName("IX_Orders_Employee");

        builder
            .HasOne(x => x.OriginalOrder)
            .WithMany(o => o.SupplementOrders)
            .HasForeignKey(x => x.OriginalOrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.OriginalOrderId)
            .HasFilter("\"OriginalOrderId\" IS NOT NULL")
            .HasDatabaseName("IX_Orders_OriginalOrder");

        builder
            .HasIndex(x => x.CounterShiftId)
            .HasFilter("\"CounterShiftId\" IS NOT NULL")
            .HasDatabaseName("IX_Orders_CounterShift");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_Orders_Status");
        builder.HasIndex(x => x.Channel).HasDatabaseName("IX_Orders_Channel");
        builder.HasIndex(x => x.Type).HasDatabaseName("IX_Orders_Type");
        builder
            .HasIndex(x => x.ExpiresAt)
            .HasFilter("\"Status\" = 'Pending'")
            .HasDatabaseName("IX_Orders_Pending_ExpiresAt");
    }
}
