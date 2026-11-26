using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class MembershipFreeTicketConfiguration : IEntityTypeConfiguration<MembershipFreeTicket>
{
    public void Configure(EntityTypeBuilder<MembershipFreeTicket> builder)
    {
        builder.ToTable(
            "MembershipFreeTickets",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_MembershipFreeTickets_PeriodRange",
                    "\"PeriodEnd\" > \"PeriodStart\" AND \"PeriodIndex\" >= 0 AND \"Sequence\" >= 1"
                );
                t.HasCheckConstraint(
                    "CK_MembershipFreeTickets_Usage",
                    "(\"UsedAt\" IS NULL AND \"OrderId\" IS NULL AND \"OrderLineId\" IS NULL AND \"ShowtimeId\" IS NULL AND \"ConvertedAmount\" IS NULL) "
                        + "OR (\"OrderId\" IS NOT NULL AND \"OrderLineId\" IS NOT NULL AND \"ShowtimeId\" IS NOT NULL AND \"ConvertedAmount\" >= 0)"
                );
            }
        );
        builder.HasKey(x => x.MembershipFreeTicketId);
        builder.Property(x => x.PeriodStart).HasColumnType("timestamp without time zone");
        builder.Property(x => x.PeriodEnd).HasColumnType("timestamp without time zone");
        builder.Property(x => x.GrantedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UsedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ConvertedAmount).HasColumnType("decimal(18,2)");

        builder
            .HasOne(x => x.MembershipSubscription)
            .WithMany()
            .HasForeignKey(x => x.MembershipSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
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
            .HasOne(x => x.Showtime)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new
            {
                x.MembershipSubscriptionId,
                x.PeriodIndex,
                x.Sequence,
            })
            .IsUnique()
            .HasDatabaseName("UX_MembershipFreeTickets_SubscriptionPeriodSequence");
        builder
            .HasIndex(x => x.OrderLineId)
            .IsUnique()
            .HasFilter("\"OrderLineId\" IS NOT NULL")
            .HasDatabaseName("UX_MembershipFreeTickets_OrderLine");
        builder
            .HasIndex(x => new { x.UserId, x.PeriodEnd })
            .HasDatabaseName("IX_MembershipFreeTickets_UserPeriodEnd");
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_MembershipFreeTickets_Order");
        builder
            .HasIndex(x => new { x.UserId, x.ShowtimeId })
            .HasFilter("\"ShowtimeId\" IS NOT NULL")
            .HasDatabaseName("IX_MembershipFreeTickets_UserShowtime");
    }
}
