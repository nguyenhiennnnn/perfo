using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class MovieCastConfiguration : IEntityTypeConfiguration<MovieCast>
{
    public void Configure(EntityTypeBuilder<MovieCast> builder)
    {
        builder.ToTable("MovieCast");
        builder.HasKey(x => new { x.MovieId, x.ActorId });
        builder.Property(x => x.CharacterName).HasMaxLength(FieldLengths.PersonName);

        builder
            .HasOne(x => x.Movie)
            .WithMany(m => m.MovieCast)
            .HasForeignKey(x => x.MovieId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Actor)
            .WithMany(a => a.MovieCast)
            .HasForeignKey(x => x.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ActorId).HasDatabaseName("IX_MovieCast_Actor");
    }
}
