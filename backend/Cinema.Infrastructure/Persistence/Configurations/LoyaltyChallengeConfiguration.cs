using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class LoyaltyChallengeConfiguration : IEntityTypeConfiguration<LoyaltyChallenge>
{
    public void Configure(EntityTypeBuilder<LoyaltyChallenge> builder)
    {
        builder.ToTable(
            "LoyaltyChallenges",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_LoyaltyChallenges_ConditionType",
                    "\"ConditionType\" IN ('DistinctMovies','Screenings')"
                );
                t.HasCheckConstraint(
                    "CK_LoyaltyChallenges_RewardType",
                    "\"RewardType\" IN ('Points','Gift')"
                );
                t.HasCheckConstraint("CK_LoyaltyChallenges_Range", "\"EndsOn\" >= \"StartsOn\"");
                t.HasCheckConstraint("CK_LoyaltyChallenges_Target", "\"TargetCount\" > 0");
                t.HasCheckConstraint(
                    "CK_LoyaltyChallenges_Reward",
                    "(\"RewardType\" = 'Points' AND \"RewardPoints\" > 0 AND \"RewardGiftId\" IS NULL) "
                        + "OR (\"RewardType\" = 'Gift' AND \"RewardGiftId\" IS NOT NULL AND \"RewardPoints\" IS NULL)"
                );
            }
        );
        builder.HasKey(x => x.ChallengeId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.ComboName).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(FieldLengths.ComboDescription);
        builder.Property(x => x.StartsOn).HasColumnType("date");
        builder.Property(x => x.EndsOn).HasColumnType("date");
        builder
            .Property(x => x.ConditionType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder
            .Property(x => x.RewardType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder
            .HasOne(x => x.RewardGift)
            .WithMany()
            .HasForeignKey(x => x.RewardGiftId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => new { x.StartsOn, x.EndsOn })
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("IX_LoyaltyChallenges_Range");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class LoyaltyChallengeProgressConfiguration
    : IEntityTypeConfiguration<LoyaltyChallengeProgress>
{
    public void Configure(EntityTypeBuilder<LoyaltyChallengeProgress> builder)
    {
        builder.ToTable(
            "LoyaltyChallengeProgresses",
            t => t.HasCheckConstraint("CK_LoyaltyChallengeProgresses_Count", "\"CurrentCount\" >= 0")
        );
        builder.HasKey(x => x.ProgressId);
        builder.Property(x => x.CountedMovieIds).HasColumnType("integer[]").HasDefaultValueSql("'{}'");
        builder
            .Property(x => x.CountedShowtimeIds)
            .HasColumnType("integer[]")
            .HasDefaultValueSql("'{}'");
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RewardGrantedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder
            .HasOne(x => x.Challenge)
            .WithMany(c => c.Progresses)
            .HasForeignKey(x => x.ChallengeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasIndex(x => new { x.ChallengeId, x.UserId })
            .IsUnique()
            .HasDatabaseName("UX_LoyaltyChallengeProgresses_Challenge_User");
        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_LoyaltyChallengeProgresses_User");
        builder.HasQueryFilter(x => !x.Challenge.IsDeleted);
    }
}
