using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ExceptionalRefundProposalConfiguration : IEntityTypeConfiguration<ExceptionalRefundProposal>
{
    public void Configure(EntityTypeBuilder<ExceptionalRefundProposal> builder)
    {
        builder.ToTable("ExceptionalRefundProposals");
        builder.HasKey(x => x.ExceptionalRefundProposalId);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ApprovalThreshold).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Reason).HasMaxLength(FieldLengths.RefundProposalReason).IsRequired();
        builder.Property(x => x.ReviewNote).HasMaxLength(FieldLengths.RefundProposalReason);
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.ProposedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ReviewedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.OrderLine)
            .WithMany()
            .HasForeignKey(x => x.OrderLineId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.ProposedByUser)
            .WithMany()
            .HasForeignKey(x => x.ProposedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.ReviewedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(x => new { x.OrderId, x.Status })
            .HasDatabaseName("IX_ExceptionalRefundProposals_Order_Status");
        builder
            .HasIndex(x => new { x.Status, x.ProposedAt })
            .HasDatabaseName("IX_ExceptionalRefundProposals_Status_ProposedAt");
    }
}
