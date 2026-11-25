using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ReviewInviteNoticeConfiguration : IEntityTypeConfiguration<ReviewInviteNotice>
{
    public void Configure(EntityTypeBuilder<ReviewInviteNotice> builder)
    {
        builder.ToTable("ReviewInviteNotices");
        builder.HasKey(x => new { x.UserId, x.MovieId });
        builder
            .Property(x => x.SentAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Movie)
            .WithMany()
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MovieId).HasDatabaseName("IX_ReviewInviteNotices_Movie");
    }
}
