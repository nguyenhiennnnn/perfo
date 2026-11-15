using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class AgeRatingConfiguration : IEntityTypeConfiguration<AgeRating>
{
    public void Configure(EntityTypeBuilder<AgeRating> builder)
    {
        builder.ToTable("AgeRatings");
        builder.HasKey(x => x.AgeRatingId);
        builder.Property(x => x.Code).HasMaxLength(FieldLengths.AgeRatingCode).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Description).HasMaxLength(FieldLengths.Description);
    }
}
