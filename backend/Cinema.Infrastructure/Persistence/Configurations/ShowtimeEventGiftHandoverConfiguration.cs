using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ShowtimeEventGiftHandoverConfiguration
    : IEntityTypeConfiguration<ShowtimeEventGiftHandover>
{
    public void Configure(EntityTypeBuilder<ShowtimeEventGiftHandover> builder)
    {
        builder.ToTable(
            "ShowtimeEventGiftHandovers",
            t => t.HasCheckConstraint("CK_ShowtimeEventGiftHandovers_Quantity", "\"Quantity\" > 0")
        );
        builder.HasKey(x => x.ShowtimeEventGiftHandoverId);
        builder.Property(x => x.HandedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Reservation)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeEventGiftReservationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.HandedByUser)
            .WithMany()
            .HasForeignKey(x => x.HandedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.ShowtimeEventGiftReservationId)
            .HasDatabaseName("IX_ShowtimeEventGiftHandovers_Reservation");
    }
}
