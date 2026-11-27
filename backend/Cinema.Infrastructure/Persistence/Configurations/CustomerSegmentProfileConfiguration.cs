using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CustomerSegmentProfileConfiguration : IEntityTypeConfiguration<CustomerSegmentProfile>
{
    public void Configure(EntityTypeBuilder<CustomerSegmentProfile> builder)
    {
        builder.ToTable(
            "CustomerSegmentProfiles",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_CustomerSegmentProfiles_Scores",
                    "\"RecencyScore\" BETWEEN 1 AND 5 AND \"FrequencyScore\" BETWEEN 1 AND 5 AND \"MonetaryScore\" BETWEEN 1 AND 5"
                );
                t.HasCheckConstraint(
                    "CK_CustomerSegmentProfiles_Amounts",
                    "\"LookbackOrderCount\" >= 0 AND \"LookbackSpent\" >= 0 AND \"TotalSpent\" >= 0 AND \"EstimatedLifetimeValue\" >= 0 AND \"ExpiringPoints\" >= 0"
                );
            }
        );
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).ValueGeneratedNever();
        builder.Property(x => x.ActivityStage).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.FirstOrderAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.LastOrderAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.PointsExpireAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.LookbackSpent).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalSpent).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EstimatedLifetimeValue).HasColumnType("decimal(18,2)");

        builder
            .HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<CustomerSegmentProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.PreferredGenre)
            .WithMany()
            .HasForeignKey(x => x.PreferredGenreId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(x => x.PreferredScreenType)
            .WithMany()
            .HasForeignKey(x => x.PreferredScreenTypeId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(x => x.NextTier)
            .WithMany()
            .HasForeignKey(x => x.NextTierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.ActivityStage).HasDatabaseName("IX_CustomerSegmentProfiles_Stage");
        builder.HasIndex(x => x.PreferredGenreId).HasDatabaseName("IX_CustomerSegmentProfiles_Genre");
        builder
            .HasIndex(x => x.PreferredScreenTypeId)
            .HasDatabaseName("IX_CustomerSegmentProfiles_ScreenType");
        builder.HasIndex(x => x.NextTierId).HasDatabaseName("IX_CustomerSegmentProfiles_NextTier");
    }
}
