using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");
        builder.HasKey(x => x.PositionId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
