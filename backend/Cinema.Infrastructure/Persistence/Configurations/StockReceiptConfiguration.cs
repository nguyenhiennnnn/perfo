using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class StockReceiptConfiguration : IEntityTypeConfiguration<StockReceipt>
{
    public void Configure(EntityTypeBuilder<StockReceipt> builder)
    {
        builder.ToTable("StockReceipts");
        builder.HasKey(x => x.StockReceiptId);
        builder.Property(x => x.SupplierName).HasMaxLength(FieldLengths.SupplierName).IsRequired();
        builder.Property(x => x.ReceivedDate).HasColumnType("date");
        builder.HasIndex(x => x.ReceivedDate);
        builder.Property(x => x.Note).HasMaxLength(FieldLengths.StockNote);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasMany(x => x.Lines)
            .WithOne(l => l.Receipt)
            .HasForeignKey(l => l.StockReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
