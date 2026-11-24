using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ReconciliationSessionConfiguration : IEntityTypeConfiguration<ReconciliationSession>
{
    public void Configure(EntityTypeBuilder<ReconciliationSession> builder)
    {
        builder.ToTable(
            "ReconciliationSessions",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_ReconciliationSessions_Gateway",
                    "\"Gateway\" IN ('VNPay','Momo','Stripe')"
                );
                t.HasCheckConstraint("CK_ReconciliationSessions_Source", "\"Source\" IN ('File','Api')");
                t.HasCheckConstraint(
                    "CK_ReconciliationSessions_Status",
                    "\"Status\" IN ('Running','Completed','Failed')"
                );
                t.HasCheckConstraint(
                    "CK_ReconciliationSessions_FileHash",
                    "\"Source\" <> 'File' OR \"FileHash\" IS NOT NULL"
                );
                t.HasCheckConstraint(
                    "CK_ReconciliationSessions_Period",
                    "\"PeriodFrom\" IS NULL OR \"PeriodTo\" IS NULL OR \"PeriodFrom\" <= \"PeriodTo\""
                );
                t.HasCheckConstraint(
                    "CK_ReconciliationSessions_Counts",
                    "\"TotalRows\" >= 0 AND \"MatchedCount\" >= 0 AND \"MissingInSystemCount\" >= 0 "
                        + "AND \"MissingAtGatewayCount\" >= 0 AND \"AmountMismatchCount\" >= 0 "
                        + "AND \"StatusMismatchCount\" >= 0 AND \"PaidAtGatewayOrderUnconfirmedCount\" >= 0"
                );
                t.HasCheckConstraint(
                    "CK_ReconciliationSessions_Amounts",
                    "\"GatewayTotalAmount\" >= 0 AND \"SystemTotalAmount\" >= 0"
                );
                t.HasCheckConstraint(
                    "CK_ReconciliationSessions_Completed",
                    "\"Status\" = 'Running' OR \"CompletedAt\" IS NOT NULL"
                );
            }
        );
        builder.HasKey(x => x.ReconciliationSessionId);
        builder
            .Property(x => x.Gateway)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder
            .Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.ReconciliationStatus.Running);
        builder.Property(x => x.FileName).HasMaxLength(FieldLengths.FileName);
        builder.Property(x => x.FileHash).HasMaxLength(FieldLengths.FileHash);
        builder.Property(x => x.PeriodFrom).HasColumnType("date");
        builder.Property(x => x.PeriodTo).HasColumnType("date");
        builder.Property(x => x.TotalRows).HasDefaultValue(0);
        builder.Property(x => x.MatchedCount).HasDefaultValue(0);
        builder.Property(x => x.MissingInSystemCount).HasDefaultValue(0);
        builder.Property(x => x.MissingAtGatewayCount).HasDefaultValue(0);
        builder.Property(x => x.AmountMismatchCount).HasDefaultValue(0);
        builder.Property(x => x.StatusMismatchCount).HasDefaultValue(0);
        builder.Property(x => x.PaidAtGatewayOrderUnconfirmedCount).HasDefaultValue(0);
        builder.Property(x => x.GatewayTotalAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.SystemTotalAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.ErrorMessage).HasMaxLength(FieldLengths.ErrorMessage);
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.Gateway, x.FileHash })
            .IsUnique()
            .HasFilter("\"Source\" = 'File'")
            .HasDatabaseName("UX_ReconciliationSessions_Gateway_FileHash");
        builder
            .HasIndex(x => new { x.Gateway, x.CreatedAt })
            .HasDatabaseName("IX_ReconciliationSessions_Gateway_CreatedAt");
    }
}
