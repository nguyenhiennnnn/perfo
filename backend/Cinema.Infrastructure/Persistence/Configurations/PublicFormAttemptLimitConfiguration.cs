using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class PublicFormAttemptLimitConfiguration : IEntityTypeConfiguration<PublicFormAttemptLimit>
{
    public void Configure(EntityTypeBuilder<PublicFormAttemptLimit> builder)
    {
        builder.ToTable("PublicFormAttemptLimit");
        builder.HasKey(x => x.Key);
        builder.Property(x => x.Key).HasMaxLength(FieldLengths.PublicFormAttemptKey);
        builder.Property(x => x.WindowStartedAt).HasColumnType("timestamp without time zone");
    }
}
