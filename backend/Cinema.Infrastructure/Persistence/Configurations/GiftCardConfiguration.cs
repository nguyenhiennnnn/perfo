using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class GiftCardConfiguration : IEntityTypeConfiguration<GiftCard>
{
    public void Configure(EntityTypeBuilder<GiftCard> builder)
    {
        builder.ToTable(
            "GiftCards",
            t =>
            {
                t.HasCheckConstraint("CK_GiftCards_Type", "\"Type\" IN ('Gift','ExchangeCredit')");
                t.HasCheckConstraint(
                    "CK_GiftCards_Status",
                    "\"Status\" IN ('PendingSend','Sent','InUse','Depleted','Expired','Cancelled')"
                );
                t.HasCheckConstraint("CK_GiftCards_FaceValue", "\"FaceValue\" > 0");
                t.HasCheckConstraint(
                    "CK_GiftCards_Balance",
                    "\"Balance\" >= 0 AND \"Balance\" <= \"FaceValue\""
                );
                t.HasCheckConstraint(
                    "CK_GiftCards_ReservedAmount",
                    "\"ReservedAmount\" >= 0 AND \"ReservedAmount\" <= \"Balance\""
                );
            }
        );
        builder.HasKey(x => x.GiftCardId);
        builder.Property(x => x.Code).HasMaxLength(FieldLengths.GiftCardCode).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.GiftCardType.Gift);
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.GiftCardStatus.PendingSend);
        builder.Property(x => x.FaceValue).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Balance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ReservedAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.RecipientEmail).HasMaxLength(FieldLengths.Email);
        builder.Property(x => x.RecipientName).HasMaxLength(FieldLengths.PersonName);
        builder.Property(x => x.Message).HasMaxLength(FieldLengths.GiftCardMessage);
        builder.Property(x => x.SendAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder
            .HasOne(x => x.PurchaserUser)
            .WithMany()
            .HasForeignKey(x => x.PurchaserUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PurchaserUserId).HasDatabaseName("IX_GiftCards_Purchaser");
        builder.HasIndex(x => x.RecipientEmail).HasDatabaseName("IX_GiftCards_RecipientEmail");
        builder.HasIndex(x => new { x.Status, x.SendAt }).HasDatabaseName("IX_GiftCards_Status_SendAt");
        builder
            .HasIndex(x => new { x.Status, x.ExpiresAt })
            .HasDatabaseName("IX_GiftCards_Status_ExpiresAt");
    }
}
