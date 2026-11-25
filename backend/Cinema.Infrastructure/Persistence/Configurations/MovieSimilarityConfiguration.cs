using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class MovieSimilarityConfiguration : IEntityTypeConfiguration<MovieSimilarity>
{
    public void Configure(EntityTypeBuilder<MovieSimilarity> builder)
    {
        builder.ToTable(
            "MovieSimilarities",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_MovieSimilarities_Distinct",
                    "\"MovieId\" <> \"SimilarMovieId\""
                );
                t.HasCheckConstraint(
                    "CK_MovieSimilarities_Values",
                    "\"Score\" >= 0 AND \"Score\" <= 1 AND \"CoViewers\" > 0"
                );
            }
        );
        builder.HasKey(x => new { x.MovieId, x.SimilarMovieId });
        builder.Property(x => x.Score).HasColumnType("decimal(9,6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.Movie)
            .WithMany()
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.SimilarMovie)
            .WithMany()
            .HasForeignKey(x => x.SimilarMovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new { x.MovieId, x.Score })
            .IsDescending(false, true)
            .HasDatabaseName("IX_MovieSimilarities_Movie_Score");
        builder.HasIndex(x => x.SimilarMovieId).HasDatabaseName("IX_MovieSimilarities_Similar");
    }
}
