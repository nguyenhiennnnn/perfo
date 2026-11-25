using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class MoviePopularityConfiguration : IEntityTypeConfiguration<MoviePopularity>
{
    public void Configure(EntityTypeBuilder<MoviePopularity> builder)
    {
        builder.ToTable(
            "MoviePopularities",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_MoviePopularities_Values",
                    "\"RecentTickets\" >= 0 AND \"ReviewCount\" >= 0 AND \"Score\" >= 0 AND \"Score\" <= 1"
                );
            }
        );
        builder.HasKey(x => x.MovieId);
        builder.Property(x => x.MovieId).ValueGeneratedNever();
        builder.Property(x => x.AverageRating).HasColumnType("decimal(4,2)");
        builder.Property(x => x.Score).HasColumnType("decimal(9,6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Movie)
            .WithOne()
            .HasForeignKey<MoviePopularity>(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Score).IsDescending().HasDatabaseName("IX_MoviePopularities_Score");
    }
}
