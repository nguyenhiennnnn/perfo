using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ReferralRelationConfiguration : IEntityTypeConfiguration<ReferralRelation>
{
    public void Configure(EntityTypeBuilder<ReferralRelation> builder)
    {
        builder.ToTable(
            "ReferralRelations",
            t =>
                t.HasCheckConstraint(
                    "CK_ReferralRelations_NotSelf",
                    "\"ReferrerUserId\" <> \"ReferredUserId\""
                )
        );
        builder.HasKey(x => x.ReferralId);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RewardGrantedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RewardBlockedAt).HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.RewardBlockedReason)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue);
        builder
            .HasOne(x => x.Referrer)
            .WithMany()
            .HasForeignKey(x => x.ReferrerUserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Referred)
            .WithMany()
            .HasForeignKey(x => x.ReferredUserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasIndex(x => x.ReferredUserId)
            .IsUnique()
            .HasDatabaseName("UX_ReferralRelations_Referred");
        builder
            .HasIndex(x => x.ReferrerUserId)
            .HasDatabaseName("IX_ReferralRelations_Referrer");
    }
}
