using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class RoomChangeRefundDueConfiguration : IEntityTypeConfiguration<RoomChangeRefundDue>
{
    public void Configure(EntityTypeBuilder<RoomChangeRefundDue> builder)
    {
        builder.ToTable(
            "RoomChangeRefundDues",
            t =>
            {
                t.HasCheckConstraint("CK_RoomChangeRefundDues_Amount", "\"AmountDue\" > 0");
                t.HasCheckConstraint(
                    "CK_RoomChangeRefundDues_Status",
                    "\"Status\" IN ('Pending','Refunded','Voided')"
                );
            }
        );
        builder.HasKey(x => x.RoomChangeRefundDueId);
        builder.Property(x => x.AmountDue).HasColumnType("decimal(18,2)");
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.RoomChangeRefundStatus.Pending);
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(x => x.RoomChange)
            .WithMany(c => c.RefundDues)
            .HasForeignKey(x => x.ShowtimeRoomChangeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.OrderLine)
            .WithMany()
            .HasForeignKey(x => x.OrderLineId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.FromSeat)
            .WithMany()
            .HasForeignKey(x => x.FromSeatId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.ToSeat)
            .WithMany()
            .HasForeignKey(x => x.ToSeatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_RoomChangeRefundDues_Order");
        builder
            .HasIndex(x => x.Status)
            .HasFilter("\"Status\" = 'Pending'")
            .HasDatabaseName("IX_RoomChangeRefundDues_Pending");
    }
}
