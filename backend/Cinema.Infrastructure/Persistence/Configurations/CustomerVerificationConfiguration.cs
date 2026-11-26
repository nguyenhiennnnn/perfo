using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CustomerVerificationConfiguration : IEntityTypeConfiguration<CustomerVerification>
{
    public void Configure(EntityTypeBuilder<CustomerVerification> builder)
    {
        builder.ToTable("CustomerVerifications");
        builder.HasKey(x => x.CustomerVerificationId);
        builder.Property(x => x.Reason).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CodeHash).HasMaxLength(FieldLengths.TokenHash);
        builder.Property(x => x.CodeSentAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CodeExpiresAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.VerifiedAt).HasColumnType("timestamp without time zone");
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter($"\"Status\" = '{nameof(CustomerVerificationStatus.Pending)}'")
            .HasDatabaseName("UQ_CustomerVerifications_User_Pending");
    }
}
