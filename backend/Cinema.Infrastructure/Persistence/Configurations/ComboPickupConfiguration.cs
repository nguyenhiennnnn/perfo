using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ComboPickupConfiguration : IEntityTypeConfiguration<ComboPickup>
{
    public void Configure(EntityTypeBuilder<ComboPickup> builder)
    {
        builder.ToTable(
            "ComboPickups",
            t => t.HasCheckConstraint("CK_ComboPickups_Quantity", "\"Quantity\" > 0")
        );
        builder.HasKey(x => x.ComboPickupId);
        builder.Property(x => x.PickedUpAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.OrderLine)
            .WithMany()
            .HasForeignKey(x => x.OrderLineId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.PickedUpByUser)
            .WithMany()
            .HasForeignKey(x => x.PickedUpByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.OrderId, x.PickedUpAt })
            .HasDatabaseName("IX_ComboPickups_Order");
        builder.HasIndex(x => x.OrderLineId).HasDatabaseName("IX_ComboPickups_OrderLine");
        builder.HasIndex(x => x.PickedUpByUserId).HasDatabaseName("IX_ComboPickups_PickedUpBy");
    }
}
