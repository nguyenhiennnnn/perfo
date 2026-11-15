using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ScreenTypeConfiguration : IEntityTypeConfiguration<ScreenType>
{
    public void Configure(EntityTypeBuilder<ScreenType> builder)
    {
        builder.ToTable("ScreenTypes");
        builder.HasKey(x => x.ScreenTypeId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
