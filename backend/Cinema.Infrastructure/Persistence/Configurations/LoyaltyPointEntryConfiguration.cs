using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class LoyaltyPointEntryConfiguration : IEntityTypeConfiguration<LoyaltyPointEntry>
{
    public void Configure(EntityTypeBuilder<LoyaltyPointEntry> builder)
    {
        builder.ToTable(
            "LoyaltyPointEntries",
            t =>
            {
                t.HasCheckConstraint("CK_LoyaltyPointEntries_Points", "\"Points\" >= 0");
                t.HasCheckConstraint(
                    "CK_LoyaltyPointEntries_Revoked",
                    "\"RevokedPoints\" >= 0 AND \"RevokedPoints\" <= \"Points\""
                );
                t.HasCheckConstraint(
                    "CK_LoyaltyPointEntries_Remaining",
                    "\"RemainingPoints\" >= 0 AND \"RemainingPoints\" <= \"Points\""
                );
            }
        );
        builder.HasKey(x => x.EntryId);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.RevokedPoints).HasDefaultValue(0);
        builder.Property(x => x.EarnedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.OrderId)
            .IsUnique()
            .HasFilter("\"OrderId\" IS NOT NULL")
            .HasDatabaseName("UX_LoyaltyPointEntries_Order");
        builder
            .HasIndex(x => new { x.UserId, x.EarnedAt })
            .HasDatabaseName("IX_LoyaltyPointEntries_User_EarnedAt");
        builder
            .HasIndex(x => new { x.UserId, x.ExpiresAt })
            .HasFilter("\"RemainingPoints\" > 0")
            .HasDatabaseName("IX_LoyaltyPointEntries_User_Lots");
    }
}
