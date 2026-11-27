using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ShowtimeEventTicketQuotaConfiguration : IEntityTypeConfiguration<ShowtimeEventTicketQuota>
{
    public void Configure(EntityTypeBuilder<ShowtimeEventTicketQuota> builder)
    {
        builder.ToTable(
            "ShowtimeEventTicketQuotas",
            t => t.HasCheckConstraint("CK_ShowtimeEventTicketQuotas_MaxTickets", "\"MaxTickets\" > 0")
        );
        builder.HasKey(x => new { x.ShowtimeId, x.TicketTypeId });

        builder
            .HasOne(x => x.EventSetting)
            .WithMany(s => s.TicketQuotas)
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.TicketType)
            .WithMany()
            .HasForeignKey(x => x.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
