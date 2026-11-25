using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class OrderExchangeConfiguration : IEntityTypeConfiguration<OrderExchange>
{
    public void Configure(EntityTypeBuilder<OrderExchange> builder)
    {
        builder.ToTable(
            "OrderExchanges",
            t =>
            {
                t.HasCheckConstraint("CK_OrderExchanges_Number", "\"ExchangeNumber\" >= 1");
                t.HasCheckConstraint(
                    "CK_OrderExchanges_Showtimes",
                    "\"OldShowtimeId\" <> \"NewShowtimeId\""
                );
                t.HasCheckConstraint(
                    "CK_OrderExchanges_Lines",
                    "\"OldOrderLineId\" <> \"NewOrderLineId\""
                );
            }
        );
        builder.HasKey(x => x.OrderExchangeId);
        builder.Property(x => x.PriceDifference).HasColumnType("decimal(18,2)");
        builder
            .Property(x => x.ExchangedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.OldOrderLine)
            .WithMany()
            .HasForeignKey(x => x.OldOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.NewOrderLine)
            .WithMany()
            .HasForeignKey(x => x.NewOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.OldShowtime)
            .WithMany()
            .HasForeignKey(x => x.OldShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.NewShowtime)
            .WithMany()
            .HasForeignKey(x => x.NewShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.PerformedByUser)
            .WithMany()
            .HasForeignKey(x => x.PerformedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(x => new { x.OrderId, x.ExchangeNumber })
            .HasDatabaseName("IX_OrderExchanges_Order");
        builder
            .HasIndex(x => x.OldOrderLineId)
            .IsUnique()
            .HasDatabaseName("UX_OrderExchanges_OldOrderLine");
        builder
            .HasIndex(x => x.NewOrderLineId)
            .IsUnique()
            .HasDatabaseName("UX_OrderExchanges_NewOrderLine");
        builder.HasIndex(x => x.NewShowtimeId).HasDatabaseName("IX_OrderExchanges_NewShowtime");
        builder.HasIndex(x => x.OldShowtimeId).HasDatabaseName("IX_OrderExchanges_OldShowtime");
    }
}
