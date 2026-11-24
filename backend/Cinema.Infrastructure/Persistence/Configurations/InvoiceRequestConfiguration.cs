using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class InvoiceRequestConfiguration : IEntityTypeConfiguration<InvoiceRequest>
{
    public void Configure(EntityTypeBuilder<InvoiceRequest> builder)
    {
        builder.ToTable("InvoiceRequests");
        builder.HasKey(x => x.InvoiceRequestId);
        builder.Property(x => x.TaxCode).HasMaxLength(FieldLengths.TaxCode).IsRequired();
        builder
            .Property(x => x.CompanyName)
            .HasMaxLength(FieldLengths.InvoiceCompanyName)
            .IsRequired();
        builder.Property(x => x.Address).HasMaxLength(FieldLengths.InvoiceAddress).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(FieldLengths.Email).IsRequired();
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.RejectReason).HasMaxLength(FieldLengths.InvoiceRejectReason);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(FieldLengths.InvoiceNumber);
        builder.Property(x => x.FileUrl).HasMaxLength(FieldLengths.Url);
        builder.Property(x => x.FilePublicId).HasMaxLength(FieldLengths.Url);
        builder.Property(x => x.FileName).HasMaxLength(FieldLengths.FileName);
        builder.Property(x => x.NeedsAdjustment).HasDefaultValue(false);
        builder.Property(x => x.AdjustmentRefundAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.AdjustmentNote).HasMaxLength(FieldLengths.InvoiceRejectReason);
        builder.Property(x => x.AdjustmentFlaggedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.AdjustmentResolvedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ProcessedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.InvoiceProfile)
            .WithMany()
            .HasForeignKey(x => x.InvoiceProfileId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(x => x.IssuedByUser)
            .WithMany()
            .HasForeignKey(x => x.IssuedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(x => x.OrderId)
            .IsUnique()
            .HasFilter("\"Status\" <> 'Rejected'")
            .HasDatabaseName("UX_InvoiceRequests_Order_Active");
        builder
            .HasIndex(x => x.AdjustmentFlaggedAt)
            .HasFilter("\"NeedsAdjustment\" = TRUE")
            .HasDatabaseName("IX_InvoiceRequests_NeedsAdjustment");
        builder
            .HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("IX_InvoiceRequests_Status_CreatedAt");
    }
}
