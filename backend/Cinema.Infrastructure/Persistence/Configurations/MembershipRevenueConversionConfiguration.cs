using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class MembershipRevenueConversionConfiguration
    : IEntityTypeConfiguration<MembershipRevenueConversion>
{
    public void Configure(EntityTypeBuilder<MembershipRevenueConversion> builder)
    {
        builder.ToTable(
            "MembershipRevenueConversions",
            t => t.HasCheckConstraint("CK_MembershipRevenueConversions_Amount", "\"Amount\" > 0")
        );
        builder.HasKey(x => x.MembershipRevenueConversionId);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.BusinessDate).HasColumnType("date");
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.MembershipFreeTicket)
            .WithMany()
            .HasForeignKey(x => x.MembershipFreeTicketId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Showtime)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.MembershipFreeTicketId)
            .IsUnique()
            .HasDatabaseName("UX_MembershipRevenueConversions_FreeTicket");
        builder.HasIndex(x => x.ShowtimeId).HasDatabaseName("IX_MembershipRevenueConversions_Showtime");
        builder.HasIndex(x => x.BusinessDate).HasDatabaseName("IX_MembershipRevenueConversions_Date");
    }
}
