using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages");
        builder.HasKey(x => x.LanguageId);
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
