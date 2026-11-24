using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ReconciliationItemConfiguration : IEntityTypeConfiguration<ReconciliationItem>
{
    public void Configure(EntityTypeBuilder<ReconciliationItem> builder)
    {
        builder.ToTable(
            "ReconciliationItems",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_ReconciliationItems_Gateway",
                    "\"Gateway\" IN ('VNPay','Momo','Stripe')"
                );
                t.HasCheckConstraint(
                    "CK_ReconciliationItems_Result",
                    "\"Result\" IN ('Matched','MissingInSystem','MissingAtGateway','AmountMismatch','StatusMismatch','PaidAtGatewayOrderUnconfirmed')"
                );
                t.HasCheckConstraint(
                    "CK_ReconciliationItems_Resolution",
                    "\"Resolution\" IN ('Open','Resolved','Ignored')"
                );
                t.HasCheckConstraint(
                    "CK_ReconciliationItems_Amounts",
                    "(\"Amount\" IS NULL OR \"Amount\" >= 0) AND \"GatewayFee\" >= 0"
                );
                t.HasCheckConstraint(
                    "CK_ReconciliationItems_PaymentTransaction",
                    "(\"Result\" = 'MissingInSystem') = (\"PaymentTransactionId\" IS NULL)"
                );
                t.HasCheckConstraint(
                    "CK_ReconciliationItems_Resolved",
                    "(\"Resolution\" = 'Open' AND \"ResolvedAt\" IS NULL AND \"ResolvedByUserId\" IS NULL) OR "
                        + "(\"Resolution\" <> 'Open' AND \"ResolvedAt\" IS NOT NULL AND \"ResolvedByUserId\" IS NOT NULL)"
                );
            }
        );
        builder.HasKey(x => x.ReconciliationItemId);
        builder
            .Property(x => x.Gateway)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder
            .Property(x => x.Result)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValueLong)
            .IsRequired();
        builder
            .Property(x => x.Resolution)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.ReconciliationResolution.Open);
        builder.Property(x => x.GatewayTransactionCode).HasMaxLength(FieldLengths.TransactionCode).IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.GatewayFee).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.GatewayStatus).HasMaxLength(FieldLengths.GatewayStatus);
        builder.Property(x => x.GatewayTime).HasColumnType("timestamp without time zone");
        builder.Property(x => x.Note).HasMaxLength(FieldLengths.ErrorMessage);
        builder.Property(x => x.ResolvedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Session)
            .WithMany(s => s.Items)
            .HasForeignKey(x => x.ReconciliationSessionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.PaymentTransaction)
            .WithMany()
            .HasForeignKey(x => x.PaymentTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.AssignedToUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.ResolvedByUser)
            .WithMany()
            .HasForeignKey(x => x.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.ReconciliationSessionId, x.GatewayTransactionCode })
            .IsUnique()
            .HasDatabaseName("UX_ReconciliationItems_Session_Code");
        builder.HasIndex(x => new { x.Resolution, x.Result }).HasDatabaseName("IX_ReconciliationItems_Resolution_Result");
        builder
            .HasIndex(x => x.PaymentTransactionId)
            .HasFilter("\"PaymentTransactionId\" IS NOT NULL")
            .HasDatabaseName("IX_ReconciliationItems_PaymentTransaction");
        builder
            .HasIndex(x => x.AssignedToUserId)
            .HasFilter("\"AssignedToUserId\" IS NOT NULL")
            .HasDatabaseName("IX_ReconciliationItems_AssignedTo");
    }
}
