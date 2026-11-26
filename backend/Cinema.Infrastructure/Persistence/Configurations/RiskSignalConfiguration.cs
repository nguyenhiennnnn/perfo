using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class RiskSignalConfiguration : IEntityTypeConfiguration<RiskSignal>
{
    public void Configure(EntityTypeBuilder<RiskSignal> builder)
    {
        builder.ToTable("RiskSignals");
        builder.HasKey(x => x.RiskSignalId);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasIndex(x => new { x.UserId, x.Type, x.CreatedAt })
            .HasDatabaseName("IX_RiskSignals_User_Type_CreatedAt");
    }
}
