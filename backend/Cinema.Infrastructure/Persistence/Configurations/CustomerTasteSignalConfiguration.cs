using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CustomerTasteSignalConfiguration : IEntityTypeConfiguration<CustomerTasteSignal>
{
    public void Configure(EntityTypeBuilder<CustomerTasteSignal> builder)
    {
        builder.ToTable("CustomerTasteSignals");
        builder.HasKey(x => new { x.UserId, x.Kind, x.RefId });
        builder
            .Property(x => x.Kind)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .IsRequired();
        builder.Property(x => x.Weight).HasColumnType("decimal(18,4)");

        builder
            .HasOne(x => x.Profile)
            .WithMany(p => p.Signals)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new { x.Kind, x.RefId })
            .HasDatabaseName("IX_CustomerTasteSignals_Kind_Ref");
    }
}
