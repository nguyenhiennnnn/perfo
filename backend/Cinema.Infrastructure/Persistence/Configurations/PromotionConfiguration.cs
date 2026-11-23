using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable(
            "Promotions",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_Promotions_Type",
                    "\"Type\" IN ('ManualCode','AutoApply','PersonalCode')"
                );
                t.HasCheckConstraint(
                    "CK_Promotions_DiscountType",
                    "\"DiscountType\" IN ('Percent','FixedAmount')"
                );
                t.HasCheckConstraint(
                    "CK_Promotions_Scope",
                    "\"Scope\" IN ('TicketsOnly','CombosOnly','WholeOrder')"
                );
                t.HasCheckConstraint(
                    "CK_Promotions_Channel",
                    "\"Channel\" IS NULL OR \"Channel\" IN ('Online','Counter')"
                );
                t.HasCheckConstraint(
                    "CK_Promotions_DiscountValue",
                    "(\"DiscountType\" = 'Percent' AND \"DiscountValue\" > 0 AND \"DiscountValue\" <= 100) "
                        + "OR (\"DiscountType\" = 'FixedAmount' AND \"DiscountValue\" > 0)"
                );
                t.HasCheckConstraint(
                    "CK_Promotions_MaxDiscount",
                    "\"MaxDiscountAmount\" IS NULL "
                        + "OR (\"DiscountType\" = 'Percent' AND \"MaxDiscountAmount\" > 0)"
                );
                t.HasCheckConstraint(
                    "CK_Promotions_MinOrder",
                    "\"MinOrderAmount\" IS NULL OR \"MinOrderAmount\" >= 0"
                );
                t.HasCheckConstraint(
                    "CK_Promotions_ValidRange",
                    "\"StartsAt\" IS NULL OR \"EndsAt\" IS NULL OR \"EndsAt\" > \"StartsAt\""
                );
                t.HasCheckConstraint(
                    "CK_Promotions_Limits",
                    "(\"TotalUsageLimit\" IS NULL OR \"TotalUsageLimit\" > 0) "
                        + "AND (\"PerCustomerLimit\" IS NULL OR \"PerCustomerLimit\" > 0) "
                        + "AND (\"BudgetAmount\" IS NULL OR \"BudgetAmount\" > 0)"
                );
                t.HasCheckConstraint("CK_Promotions_Priority", "\"Priority\" >= 0");
                t.HasCheckConstraint(
                    "CK_Promotions_Assignment",
                    "(\"Type\" = 'PersonalCode') = (\"AssignedUserId\" IS NOT NULL)"
                );
            }
        );
        builder.HasKey(x => x.PromotionId);
        builder.Property(x => x.Code).HasMaxLength(FieldLengths.PromotionCode).IsRequired();
        builder
            .HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("UX_Promotions_Code");
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.ComboName).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(FieldLengths.ComboDescription);
        builder
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder
            .Property(x => x.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.DiscountValue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MaxDiscountAmount).HasColumnType("decimal(18,2)");
        builder
            .Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.MinOrderAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.StartsAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.EndsAt).HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.Channel)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue);
        builder.Property(x => x.MovieIds).HasColumnType("integer[]").HasDefaultValueSql("'{}'");
        builder.Property(x => x.ShowtimeIds).HasColumnType("integer[]").HasDefaultValueSql("'{}'");
        builder
            .Property(x => x.ScreenTypeIds)
            .HasColumnType("integer[]")
            .HasDefaultValueSql("'{}'");
        builder.Property(x => x.DaysOfWeek).HasColumnType("integer[]").HasDefaultValueSql("'{}'");
        builder
            .Property(x => x.MembershipTierIds)
            .HasColumnType("integer[]")
            .HasDefaultValueSql("'{}'");
        builder.Property(x => x.NewCustomersOnly).HasDefaultValue(false);
        builder.Property(x => x.BudgetAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Priority).HasDefaultValue(0);
        builder.Property(x => x.IsExclusive).HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.AssignedUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => x.AssignedUserId)
            .HasFilter("\"AssignedUserId\" IS NOT NULL AND \"IsDeleted\" = FALSE")
            .HasDatabaseName("IX_Promotions_AssignedUser");
        builder
            .HasIndex(x => new { x.Type, x.IsActive })
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("IX_Promotions_Type_Active");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
