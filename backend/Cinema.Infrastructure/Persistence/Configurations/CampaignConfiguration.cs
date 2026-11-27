using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable(
            "Campaigns",
            t =>
            {
                t.HasCheckConstraint("CK_Campaigns_Channel", "\"Channel\" IN ('Email','InApp')");
                t.HasCheckConstraint(
                    "CK_Campaigns_Status",
                    "\"Status\" IN ('Draft','Scheduled','Sending','Completed')"
                );
                t.HasCheckConstraint(
                    "CK_Campaigns_PromoDiscountType",
                    "\"PromoDiscountType\" IS NULL OR \"PromoDiscountType\" IN ('Percent','FixedAmount')"
                );
                t.HasCheckConstraint(
                    "CK_Campaigns_Promo",
                    "(\"PromoDiscountType\" IS NULL AND \"PromoDiscountValue\" IS NULL "
                        + "AND \"PromoMaxDiscountAmount\" IS NULL AND \"PromoValidityDays\" IS NULL) "
                        + "OR (\"PromoDiscountType\" = 'Percent' AND \"PromoDiscountValue\" > 0 "
                        + "AND \"PromoDiscountValue\" <= 100 AND (\"PromoMaxDiscountAmount\" IS NULL "
                        + "OR \"PromoMaxDiscountAmount\" > 0) AND \"PromoValidityDays\" > 0) "
                        + "OR (\"PromoDiscountType\" = 'FixedAmount' AND \"PromoDiscountValue\" > 0 "
                        + "AND \"PromoMaxDiscountAmount\" IS NULL AND \"PromoValidityDays\" > 0)"
                );
            }
        );
        builder.HasKey(x => x.CampaignId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.CampaignName).IsRequired();
        builder
            .Property(x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(FieldLengths.CampaignSubject).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(FieldLengths.CampaignBody).IsRequired();
        builder
            .Property(x => x.PromoDiscountType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue);
        builder.Property(x => x.PromoDiscountValue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PromoMaxDiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ScheduledAt).HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder
            .HasOne<CustomerSegment>()
            .WithMany()
            .HasForeignKey(x => x.SegmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => new { x.Status, x.ScheduledAt })
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("IX_Campaigns_Status_ScheduledAt");
        builder.HasIndex(x => x.SegmentId).HasDatabaseName("IX_Campaigns_Segment");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
