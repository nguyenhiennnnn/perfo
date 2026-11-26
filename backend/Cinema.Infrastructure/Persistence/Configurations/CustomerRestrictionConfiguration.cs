using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CustomerRestrictionConfiguration : IEntityTypeConfiguration<CustomerRestriction>
{
    public void Configure(EntityTypeBuilder<CustomerRestriction> builder)
    {
        builder.ToTable("CustomerRestrictions");
        builder.HasKey(x => x.CustomerRestrictionId);
        builder.Property(x => x.Reason).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.StartedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ExpiresAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.LiftedAt).HasColumnType("timestamp without time zone");
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.LiftedBy)
            .WithMany()
            .HasForeignKey(x => x.LiftedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter($"\"Status\" = '{nameof(CustomerRestrictionStatus.Active)}'")
            .HasDatabaseName("UQ_CustomerRestrictions_User_Active");
        builder
            .HasIndex(x => new { x.Status, x.ExpiresAt })
            .HasDatabaseName("IX_CustomerRestrictions_Status_ExpiresAt");
    }
}
