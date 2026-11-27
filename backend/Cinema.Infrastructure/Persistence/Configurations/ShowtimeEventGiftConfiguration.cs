using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ShowtimeEventGiftConfiguration : IEntityTypeConfiguration<ShowtimeEventGift>
{
    public void Configure(EntityTypeBuilder<ShowtimeEventGift> builder)
    {
        builder.ToTable(
            "ShowtimeEventGifts",
            t =>
            {
                t.HasCheckConstraint("CK_ShowtimeEventGifts_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint(
                    "CK_ShowtimeEventGifts_QuantityPerTicket",
                    "\"QuantityPerTicket\" > 0"
                );
            }
        );
        builder.HasKey(x => new { x.ShowtimeId, x.InventoryItemId });
        builder.Property(x => x.QuantityPerTicket).HasDefaultValue(1);

        builder
            .HasOne(x => x.EventSetting)
            .WithMany(s => s.Gifts)
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.InventoryItem)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
