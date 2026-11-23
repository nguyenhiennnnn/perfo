using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        builder.ToTable(
            "PricingRules",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_PricingRules_ValidRange",
                    "\"ValidFrom\" IS NULL OR \"ValidTo\" IS NULL OR \"ValidTo\" >= \"ValidFrom\""
                );
                t.HasCheckConstraint(
                    "CK_PricingRules_AdjustmentValue",
                    "(\"RuleType\" = 'Occupancy' AND \"AdjustmentValue\" IS NULL) "
                        + "OR (\"RuleType\" <> 'Occupancy' AND \"AdjustmentValue\" IS NOT NULL)"
                );
            }
        );
        builder.HasKey(x => x.PricingRuleId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.ComboName).IsRequired();
        builder
            .HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("UX_PricingRules_Name");
        builder
            .Property(x => x.RuleType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.ConditionJson).HasColumnType("jsonb").IsRequired();
        builder
            .Property(x => x.AdjustmentMode)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.AdjustmentValue).HasColumnType("decimal(18,4)");
        builder.Property(x => x.Priority).HasDefaultValue(0);
        builder.Property(x => x.ValidFrom).HasColumnType("date");
        builder.Property(x => x.ValidTo).HasColumnType("date");
        builder.Property(x => x.RoomIds).HasColumnType("integer[]").HasDefaultValueSql("'{}'");
        builder
            .Property(x => x.ScreenTypeIds)
            .HasColumnType("integer[]")
            .HasDefaultValueSql("'{}'");
        builder.Property(x => x.MovieIds).HasColumnType("integer[]").HasDefaultValueSql("'{}'");
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder
            .HasIndex(x => new { x.RuleType, x.IsActive })
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("IX_PricingRules_Type_Active");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
