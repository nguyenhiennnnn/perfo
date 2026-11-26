using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class BundleItemConfiguration : IEntityTypeConfiguration<BundleItem>
{
    public void Configure(EntityTypeBuilder<BundleItem> builder)
    {
        builder.ToTable(
            "BundleItems",
            t =>
            {
                t.HasCheckConstraint("CK_BundleItems_Quantity", "\"Quantity\" > 0");
                t.HasCheckConstraint(
                    "CK_BundleItems_Target",
                    "(\"TicketTypeId\" IS NOT NULL) <> (\"ComboId\" IS NOT NULL)"
                );
            }
        );
        builder.HasKey(x => x.BundleItemId);
        builder
            .HasIndex(x => new { x.BundleId, x.TicketTypeId })
            .IsUnique()
            .HasFilter("\"TicketTypeId\" IS NOT NULL")
            .HasDatabaseName("UX_BundleItems_Bundle_TicketType");
        builder
            .HasIndex(x => new { x.BundleId, x.ComboId })
            .IsUnique()
            .HasFilter("\"ComboId\" IS NOT NULL")
            .HasDatabaseName("UX_BundleItems_Bundle_Combo");
        builder.HasIndex(x => x.TicketTypeId);
        builder.HasIndex(x => x.ComboId);

        builder
            .HasOne(x => x.TicketType)
            .WithMany()
            .HasForeignKey(x => x.TicketTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Combo)
            .WithMany()
            .HasForeignKey(x => x.ComboId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.Bundle.IsDeleted);
    }
}
