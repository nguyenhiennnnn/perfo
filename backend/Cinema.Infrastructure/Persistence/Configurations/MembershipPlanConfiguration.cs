using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class MembershipPlanConfiguration : IEntityTypeConfiguration<MembershipPlan>
{
    public void Configure(EntityTypeBuilder<MembershipPlan> builder)
    {
        builder.ToTable(
            "MembershipPlans",
            t =>
            {
                t.HasCheckConstraint("CK_MembershipPlans_Period", "\"Period\" IN ('Monthly','Quarterly')");
                t.HasCheckConstraint("CK_MembershipPlans_Price", "\"Price\" > 0");
                t.HasCheckConstraint(
                    "CK_MembershipPlans_TicketDiscountPercent",
                    "\"TicketDiscountPercent\" >= 0 AND \"TicketDiscountPercent\" <= 100"
                );
                t.HasCheckConstraint(
                    "CK_MembershipPlans_ComboDiscountPercent",
                    "\"ComboDiscountPercent\" >= 0 AND \"ComboDiscountPercent\" <= 100"
                );
                t.HasCheckConstraint(
                    "CK_MembershipPlans_FreeTicketsPerMonth",
                    "\"FreeTicketsPerMonth\" >= 0"
                );
                t.HasCheckConstraint(
                    "CK_MembershipPlans_FreeTicketConversionValue",
                    "\"FreeTicketConversionValue\" >= 0"
                );
            }
        );
        builder.HasKey(x => x.MembershipPlanId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.ComboName).IsRequired();
        builder
            .HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("UX_MembershipPlans_Name");
        builder.Property(x => x.Description).HasMaxLength(FieldLengths.ComboDescription);
        builder
            .Property(x => x.Period)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue);
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TicketDiscountPercent).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
        builder.Property(x => x.FreeTicketsPerMonth).HasDefaultValue(0);
        builder.Property(x => x.FreeTicketConversionValue).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.ComboDiscountPercent).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
        builder.Property(x => x.EarlyAccess).HasDefaultValue(false);
        builder.Property(x => x.StackableWithPromotions).HasDefaultValue(false);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
