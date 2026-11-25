using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class LoyaltyPointTransactionConfiguration : IEntityTypeConfiguration<LoyaltyPointTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyPointTransaction> builder)
    {
        builder.ToTable(
            "LoyaltyPointTransactions",
            t => t.HasCheckConstraint("CK_LoyaltyPointTransactions_Points", "\"Points\" <> 0")
        );
        builder.HasKey(x => x.TransactionId);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Entry)
            .WithMany()
            .HasForeignKey(x => x.EntryId)
            .OnDelete(DeleteBehavior.NoAction);
        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("IX_LoyaltyPointTransactions_User_CreatedAt");
        builder.HasIndex(x => x.EntryId).HasDatabaseName("IX_LoyaltyPointTransactions_Entry");
    }
}
