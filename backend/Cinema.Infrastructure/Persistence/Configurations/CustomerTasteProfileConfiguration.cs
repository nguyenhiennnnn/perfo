using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CustomerTasteProfileConfiguration : IEntityTypeConfiguration<CustomerTasteProfile>
{
    public void Configure(EntityTypeBuilder<CustomerTasteProfile> builder)
    {
        builder.ToTable("CustomerTasteProfiles");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).ValueGeneratedNever();
        builder.Property(x => x.TypicalTicketPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<CustomerTasteProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
