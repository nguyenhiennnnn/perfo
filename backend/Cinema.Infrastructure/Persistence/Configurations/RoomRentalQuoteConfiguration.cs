using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class RoomRentalQuoteConfiguration : IEntityTypeConfiguration<RoomRentalQuote>
{
    public void Configure(EntityTypeBuilder<RoomRentalQuote> builder)
    {
        builder.ToTable(
            "RoomRentalQuotes",
            t =>
            {
                t.HasCheckConstraint("CK_RoomRentalQuotes_Version", "\"VersionNumber\" >= 1");
                t.HasCheckConstraint(
                    "CK_RoomRentalQuotes_Amounts",
                    "\"Total\" >= 0 AND \"DepositAmount\" >= 0 AND \"DepositAmount\" <= \"Total\""
                );
                t.HasCheckConstraint(
                    "CK_RoomRentalQuotes_DepositPercent",
                    "\"DepositPercent\" > 0 AND \"DepositPercent\" <= 100"
                );
            }
        );
        builder.HasKey(x => x.RoomRentalQuoteId);
        builder.Property(x => x.RoomName).HasMaxLength(FieldLengths.CinemaName).IsRequired();
        builder.Property(x => x.RoomRentalRateName).HasMaxLength(FieldLengths.ComboName).IsRequired();
        builder.Property(x => x.StartAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.Subtotal).HasColumnType("decimal(18,0)");
        builder.Property(x => x.DiscountTotal).HasColumnType("decimal(18,0)");
        builder.Property(x => x.Total).HasColumnType("decimal(18,0)");
        builder.Property(x => x.DepositPercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.DepositAmount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.ValidUntil).HasColumnType("timestamp without time zone");
        builder.Property(x => x.BalanceDueAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CancellationPolicyJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Note).HasMaxLength(FieldLengths.ComboDescription);
        builder.Property(x => x.SentAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.SentToEmail).HasMaxLength(FieldLengths.Email);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder
            .HasOne(x => x.Request)
            .WithMany()
            .HasForeignKey(x => x.RoomRentalRequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasIndex(x => new { x.RoomRentalRequestId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UX_RoomRentalQuotes_Request_Version");
    }
}

public class RoomRentalQuoteLineConfiguration : IEntityTypeConfiguration<RoomRentalQuoteLine>
{
    public void Configure(EntityTypeBuilder<RoomRentalQuoteLine> builder)
    {
        builder.ToTable(
            "RoomRentalQuoteLines",
            t => t.HasCheckConstraint("CK_RoomRentalQuoteLines_Quantity", "\"Quantity\" >= 1")
        );
        builder.HasKey(x => x.RoomRentalQuoteLineId);
        builder
            .Property(x => x.Kind)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.Label).HasMaxLength(FieldLengths.ComboDescription).IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,0)");
        builder.Property(x => x.Amount).HasColumnType("decimal(18,0)");
        builder
            .HasOne(x => x.Quote)
            .WithMany(x => x.Lines)
            .HasForeignKey(x => x.RoomRentalQuoteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasIndex(x => new { x.RoomRentalQuoteId, x.SortOrder })
            .IsUnique()
            .HasDatabaseName("UX_RoomRentalQuoteLines_Quote_Sort");
    }
}
