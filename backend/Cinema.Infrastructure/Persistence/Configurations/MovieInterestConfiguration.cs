using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class MovieInterestConfiguration : IEntityTypeConfiguration<MovieInterest>
{
    public void Configure(EntityTypeBuilder<MovieInterest> builder)
    {
        builder.ToTable("MovieInterests");
        builder.HasKey(x => new { x.UserId, x.MovieId });
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.NotifiedAt).HasColumnType("timestamp without time zone");

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

        builder.HasIndex(x => x.MovieId).HasDatabaseName("IX_MovieInterests_Movie");
        builder
            .HasIndex(x => new { x.UserId, x.CreatedAt })
            .HasDatabaseName("IX_MovieInterests_User_CreatedAt");
    }
}
