using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ReviewPointRewardConfiguration : IEntityTypeConfiguration<ReviewPointReward>
{
    public void Configure(EntityTypeBuilder<ReviewPointReward> builder)
    {
        builder.ToTable(
            "ReviewPointRewards",
            t => t.HasCheckConstraint("CK_ReviewPointRewards_Points", "\"Points\" > 0")
        );
        builder.HasKey(x => new { x.UserId, x.MovieId });
        builder
            .Property(x => x.GrantedAt)
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

        builder.HasIndex(x => x.MovieId).HasDatabaseName("IX_ReviewPointRewards_Movie");
    }
}
