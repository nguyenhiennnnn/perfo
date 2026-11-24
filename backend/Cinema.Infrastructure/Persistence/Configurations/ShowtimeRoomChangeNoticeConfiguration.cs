using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ShowtimeRoomChangeNoticeConfiguration : IEntityTypeConfiguration<ShowtimeRoomChangeNotice>
{
    public void Configure(EntityTypeBuilder<ShowtimeRoomChangeNotice> builder)
    {
        builder.ToTable("ShowtimeRoomChangeNotices");
        builder.HasKey(x => new { x.ShowtimeRoomChangeId, x.OrderId });
        builder
            .Property(x => x.SentAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(x => x.RoomChange)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeRoomChangeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_ShowtimeRoomChangeNotices_Order");
    }
}
