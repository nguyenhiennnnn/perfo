using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class RoomClosureConfiguration : IEntityTypeConfiguration<RoomClosure>
{
    public void Configure(EntityTypeBuilder<RoomClosure> builder)
    {
        builder.ToTable(
            "RoomClosures",
            t => t.HasCheckConstraint("CK_RoomClosures_Time", "\"EndTime\" > \"StartTime\"")
        );
        builder.HasKey(x => x.RoomClosureId);
        builder.Property(x => x.StartTime).HasColumnType("timestamp without time zone");
        builder.Property(x => x.EndTime).HasColumnType("timestamp without time zone");
        builder.Property(x => x.Reason).HasMaxLength(FieldLengths.RoomClosureReason).IsRequired();
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(x => x.Room)
            .WithMany(r => r.Closures)
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new
            {
                x.RoomId,
                x.StartTime,
                x.EndTime,
            })
            .HasDatabaseName("IX_RoomClosures_Room_Time");
    }
}
