using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class MembershipSubscriptionConfiguration : IEntityTypeConfiguration<MembershipSubscription>
{
    public void Configure(EntityTypeBuilder<MembershipSubscription> builder)
    {
        builder.ToTable(
            "MembershipSubscriptions",
            t =>
            {
                t.HasCheckConstraint("CK_MembershipSubscriptions_ValidRange", "\"ValidTo\" > \"ValidFrom\"");
                t.HasCheckConstraint(
                    "CK_MembershipSubscriptions_Status",
                    "\"Status\" IN ('Active','Expired')"
                );
            }
        );
        builder.HasKey(x => x.MembershipSubscriptionId);
        builder.Property(x => x.ValidFrom).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ValidTo).HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(MembershipSubscriptionStatus.Active);
        builder
            .Property(x => x.ExpiryReminderSentAt)
            .HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.MembershipPlan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(x => x.MembershipPlanId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId).IsUnique().HasDatabaseName("UX_MembershipSubscriptions_Order");
        builder
            .HasIndex(x => new { x.UserId, x.ValidFrom })
            .IsUnique()
            .HasDatabaseName("UX_MembershipSubscriptions_UserValidFrom");
        builder
            .HasIndex(x => new { x.UserId, x.ValidTo })
            .HasDatabaseName("IX_MembershipSubscriptions_UserValidTo");
        builder
            .HasIndex(x => new { x.Status, x.ValidTo })
            .HasDatabaseName("IX_MembershipSubscriptions_Status_ValidTo");
        builder.HasIndex(x => x.MembershipPlanId).HasDatabaseName("IX_MembershipSubscriptions_Plan");
    }
}
