using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class OrderResourceHoldConfiguration : IEntityTypeConfiguration<OrderResourceHold>
{
    public void Configure(EntityTypeBuilder<OrderResourceHold> builder)
    {
        builder.ToTable(
            "OrderResourceHolds",
            t =>
            {
                t.HasCheckConstraint("CK_OrderResourceHolds_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint(
                    "CK_OrderResourceHolds_Status",
                    "\"Status\" IN ('Reserved','Committed','Released')"
                );
                t.HasCheckConstraint(
                    "CK_OrderResourceHolds_Resolved",
                    "(\"Status\" = 'Reserved') = (\"ResolvedAt\" IS NULL)"
                );
            }
        );
        builder.HasKey(x => x.ResourceHoldId);
        builder
            .Property(x => x.ResourceType)
            .HasMaxLength(FieldLengths.ReferenceType)
            .IsRequired();
        builder.Property(x => x.Quantity).HasDefaultValue(1);
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
            .WithMany(o => o.ResourceHolds)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasIndex(x => new
            {
                x.OrderId,
                x.ResourceType,
                x.Status,
            })
            .HasDatabaseName("IX_OrderResourceHolds_Order");
        builder
            .HasIndex(x => new
            {
                x.ResourceType,
                x.ResourceId,
                x.Status,
            })
            .HasFilter("\"Status\" = 'Reserved'")
            .HasDatabaseName("IX_OrderResourceHolds_Reserved");
    }
}
