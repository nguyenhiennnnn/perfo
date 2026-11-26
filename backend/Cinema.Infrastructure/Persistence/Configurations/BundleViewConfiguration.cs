using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class BundleViewConfiguration : IEntityTypeConfiguration<BundleView>
{
    public void Configure(EntityTypeBuilder<BundleView> builder)
    {
        builder.ToTable("BundleViews");
        builder.HasKey(x => new { x.BundleId, x.ShowtimeId, x.UserId });

        builder
            .HasOne(x => x.Bundle)
            .WithMany()
            .HasForeignKey(x => x.BundleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.Showtime)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.ShowtimeId);
        builder.HasQueryFilter(x => !x.Bundle.IsDeleted);
    }
}
