using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CampaignRecipientConfiguration : IEntityTypeConfiguration<CampaignRecipient>
{
    public void Configure(EntityTypeBuilder<CampaignRecipient> builder)
    {
        builder.ToTable(
            "CampaignRecipients",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_CampaignRecipients_Status",
                    "\"Status\" IN ('Pending','Sent','Failed')"
                );
            }
        );
        builder.HasKey(x => new { x.CampaignId, x.UserId });
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.SentAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Campaign)
            .WithMany(x => x.Recipients)
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Promotion)
            .WithMany()
            .HasForeignKey(x => x.PromotionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("IX_CampaignRecipients_User_CreatedAt");
        builder
            .HasIndex(x => new { x.CampaignId, x.Status })
            .HasDatabaseName("IX_CampaignRecipients_Campaign_Status");
    }
}
