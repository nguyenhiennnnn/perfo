using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class SupportRequestConfiguration : IEntityTypeConfiguration<SupportRequest>
{
    public void Configure(EntityTypeBuilder<SupportRequest> builder)
    {
        builder.ToTable("SupportRequests");
        builder.HasKey(x => x.SupportRequestId);
        builder
            .Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(FieldLengths.SupportRequestSubject).IsRequired();
        builder.Property(x => x.DueAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.LastMessageAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.ResolvedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(x => x.AssignedToUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder
            .HasOne(x => x.ApologyPromotion)
            .WithMany()
            .HasForeignKey(x => x.ApologyPromotionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(x => new { x.UserId, x.LastMessageAt })
            .HasDatabaseName("IX_SupportRequests_User_LastMessageAt");
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_SupportRequests_OrderId");
        builder
            .HasIndex(x => new { x.Status, x.DueAt })
            .HasDatabaseName("IX_SupportRequests_Status_DueAt");
        builder
            .HasIndex(x => x.AssignedToUserId)
            .HasDatabaseName("IX_SupportRequests_AssignedToUserId");
    }
}

public class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> builder)
    {
        builder.ToTable("SupportMessages");
        builder.HasKey(x => x.SupportMessageId);
        builder
            .Property(x => x.AuthorType)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder
            .Property(x => x.Content)
            .HasMaxLength(FieldLengths.SupportMessageContent)
            .IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.SupportRequest)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.SupportRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.AuthorUser)
            .WithMany()
            .HasForeignKey(x => x.AuthorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasIndex(x => new { x.SupportRequestId, x.CreatedAt })
            .HasDatabaseName("IX_SupportMessages_Request_CreatedAt");
    }
}
