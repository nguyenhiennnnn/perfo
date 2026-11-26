using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class BundleReservationConfiguration : IEntityTypeConfiguration<BundleReservation>
{
    public void Configure(EntityTypeBuilder<BundleReservation> builder)
    {
        builder.ToTable(
            "BundleReservations",
            t =>
            {
                t.HasCheckConstraint("CK_BundleReservations_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint(
                    "CK_BundleReservations_Status",
                    "\"Status\" IN ('Reserved','Committed','Released')"
                );
                t.HasCheckConstraint(
                    "CK_BundleReservations_Resolved",
                    "(\"Status\" = 'Reserved') = (\"ResolvedAt\" IS NULL)"
                );
            }
        );
        builder.HasKey(x => x.BundleReservationId);
        builder.Property(x => x.BusinessDate).HasColumnType("date");
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.ResourceHoldStatus.Reserved);
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.ResolvedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Bundle)
            .WithMany()
            .HasForeignKey(x => x.BundleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.OrderId, x.BundleId })
            .IsUnique()
            .HasDatabaseName("UX_BundleReservations_Order_Bundle");
        builder
            .HasIndex(x => new { x.BundleId, x.BusinessDate, x.Status })
            .HasDatabaseName("IX_BundleReservations_Bundle_Date");
        builder.HasQueryFilter(x => !x.Bundle.IsDeleted);
    }
}
