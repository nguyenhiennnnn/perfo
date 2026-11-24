using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class PaymentDailyStatConfiguration : IEntityTypeConfiguration<PaymentDailyStat>
{
    public void Configure(EntityTypeBuilder<PaymentDailyStat> builder)
    {
        builder.ToTable(
            "PaymentDailyStats",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_PaymentDailyStats_Channel",
                    "\"Channel\" IN ('Online','Counter')"
                );
                t.HasCheckConstraint(
                    "CK_PaymentDailyStats_Method",
                    "\"Method\" IN ('VNPay','Momo','Cash','Stripe','GiftCard','BankTransfer','Card')"
                );
                t.HasCheckConstraint(
                    "CK_PaymentDailyStats_Amounts",
                    "\"TransactionCount\" >= 0 AND \"CollectedAmount\" >= 0 AND \"RefundedAmount\" >= 0 AND \"GatewayFee\" >= 0"
                );
            }
        );
        builder.HasKey(x => new
        {
            x.BusinessDate,
            x.Channel,
            x.Method,
        });
        builder.Property(x => x.BusinessDate).HasColumnType("date");
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(FieldLengths.EnumValue);
        builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(FieldLengths.EnumValue);
        builder.Property(x => x.CollectedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RefundedAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.GatewayFee).HasColumnType("decimal(18,2)");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
    }
}
