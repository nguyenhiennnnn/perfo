using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CustomerDeviceConfiguration : IEntityTypeConfiguration<CustomerDevice>
{
    public void Configure(EntityTypeBuilder<CustomerDevice> builder)
    {
        builder.ToTable("CustomerDevices");
        builder.HasKey(x => new { x.UserId, x.DeviceId });
        builder.Property(x => x.DeviceId).HasMaxLength(FieldLengths.ClientDeviceId);
        builder.Property(x => x.FirstSeenAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.LastSeenAt).HasColumnType("timestamp without time zone");
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.DeviceId).HasDatabaseName("IX_CustomerDevices_DeviceId");
    }
}
