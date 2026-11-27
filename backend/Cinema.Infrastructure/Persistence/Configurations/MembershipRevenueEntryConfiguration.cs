using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class MembershipRevenueEntryConfiguration : IEntityTypeConfiguration<MembershipRevenueEntry>
{
    public void Configure(EntityTypeBuilder<MembershipRevenueEntry> builder)
    {
        builder.ToTable(
            "MembershipRevenueEntries",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_MembershipRevenueEntries_Range",
                    "\"PeriodEnd\" > \"PeriodStart\" AND \"MonthIndex\" >= 0 AND \"Amount\" >= 0"
                );
            }
        );
        builder.HasKey(x => x.MembershipRevenueEntryId);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PeriodStart).HasColumnType("timestamp without time zone");
        builder.Property(x => x.PeriodEnd).HasColumnType("timestamp without time zone");
        builder.Property(x => x.BusinessDate).HasColumnType("date");
        builder.Property(x => x.OccurredAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.MembershipSubscription)
            .WithMany()
            .HasForeignKey(x => x.MembershipSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.MembershipSubscriptionId, x.MonthIndex })
            .IsUnique()
            .HasDatabaseName("UX_MembershipRevenueEntries_SubscriptionMonth");
        builder.HasIndex(x => x.BusinessDate).HasDatabaseName("IX_MembershipRevenueEntries_Date");
    }
}
