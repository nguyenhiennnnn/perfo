using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class RoomRentalRequestConfiguration : IEntityTypeConfiguration<RoomRentalRequest>
{
    public void Configure(EntityTypeBuilder<RoomRentalRequest> builder)
    {
        builder.ToTable("RoomRentalRequests");
        builder.HasKey(x => x.RoomRentalRequestId);
        builder
            .Property(x => x.RequestCode)
            .HasMaxLength(FieldLengths.RoomRentalRequestCode)
            .IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(FieldLengths.FullName).IsRequired();
        builder.Property(x => x.OrganizationName).HasMaxLength(FieldLengths.RoomRentalOrganization);
        builder
            .Property(x => x.AudienceType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.ContactPhone).HasMaxLength(FieldLengths.Phone).IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(FieldLengths.Email).IsRequired();
        builder.Property(x => x.DesiredStartAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.FlexibleFromAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.FlexibleToAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ContentNote).HasMaxLength(FieldLengths.RoomRentalNote);
        builder.Property(x => x.ComboRequest).HasMaxLength(FieldLengths.RoomRentalNote);
        builder.Property(x => x.Notes).HasMaxLength(FieldLengths.RoomRentalNote);
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.StatusNote).HasMaxLength(FieldLengths.RoomRentalNote);
        builder
            .Property(x => x.TrackingTokenHash)
            .HasMaxLength(FieldLengths.TokenHash)
            .IsRequired();
        builder
            .Property(x => x.TrackingTokenExpiresAt)
            .HasColumnType("timestamp without time zone");
        builder.Property(x => x.ClientIpAddress).HasMaxLength(FieldLengths.IpAddress);
        builder.Property(x => x.ClientDeviceId).HasMaxLength(FieldLengths.ClientDeviceId);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Movie)
            .WithMany()
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(x => x.ScreenType)
            .WithMany()
            .HasForeignKey(x => x.ScreenTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(x => x.RequestCode)
            .IsUnique()
            .HasDatabaseName("UX_RoomRentalRequests_RequestCode");
        builder
            .HasIndex(x => x.TrackingTokenHash)
            .IsUnique()
            .HasDatabaseName("UX_RoomRentalRequests_TrackingTokenHash");
        builder
            .HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("IX_RoomRentalRequests_Status_CreatedAt");
    }
}
