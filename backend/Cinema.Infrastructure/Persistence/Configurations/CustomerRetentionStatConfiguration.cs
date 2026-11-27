using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CustomerRetentionStatConfiguration : IEntityTypeConfiguration<CustomerRetentionStat>
{
    public void Configure(EntityTypeBuilder<CustomerRetentionStat> builder)
    {
        builder.ToTable(
            "CustomerRetentionStats",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_CustomerRetentionStats_Values",
                    "\"MonthOffset\" >= 1 AND \"CohortSize\" >= 0 AND \"ReturnedCustomers\" >= 0 AND \"ReturnedCustomers\" <= \"CohortSize\""
                );
            }
        );
        builder.HasKey(x => new { x.CohortMonth, x.MonthOffset });
        builder.Property(x => x.CohortMonth).HasColumnType("date");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
    }
}
