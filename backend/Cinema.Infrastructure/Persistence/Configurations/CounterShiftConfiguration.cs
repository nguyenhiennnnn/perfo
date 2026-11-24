using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class CounterShiftConfiguration : IEntityTypeConfiguration<CounterShift>
{
    public void Configure(EntityTypeBuilder<CounterShift> builder)
    {
        builder.ToTable(
            "CounterShifts",
            t =>
            {
                t.HasCheckConstraint("CK_CounterShifts_Status", "\"Status\" IN ('Open','Closed')");
                t.HasCheckConstraint("CK_CounterShifts_OpeningCash", "\"OpeningCash\" >= 0");
                t.HasCheckConstraint(
                    "CK_CounterShifts_Closure",
                    "(\"Status\" = 'Open' AND \"ClosedAt\" IS NULL AND \"CountedCash\" IS NULL "
                        + "AND \"ExpectedCash\" IS NULL AND \"Variance\" IS NULL) OR "
                        + "(\"Status\" = 'Closed' AND \"ClosedAt\" IS NOT NULL AND \"ClosedAt\" >= \"OpenedAt\" "
                        + "AND \"CountedCash\" IS NOT NULL AND \"CountedCash\" >= 0 "
                        + "AND \"ExpectedCash\" IS NOT NULL AND \"Variance\" IS NOT NULL "
                        + "AND \"Variance\" = \"CountedCash\" - \"ExpectedCash\")"
                );
            }
        );
        builder.HasKey(x => x.CounterShiftId);
        builder
            .Property(x => x.OpenedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");
        builder.Property(x => x.OpeningCash).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ClosedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.CountedCash).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ExpectedCash).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Variance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Note).HasMaxLength(FieldLengths.CounterShiftNote);
        builder.Property(x => x.UnclosedAlertedAt).HasColumnType("timestamp without time zone");
        builder
            .Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(FieldLengths.EnumValue)
            .HasDefaultValue(Domain.Enums.CounterShiftStatus.Open);
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder
            .HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(x => x.Orders)
            .WithOne()
            .HasForeignKey(o => o.CounterShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => x.EmployeeId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Open'")
            .HasDatabaseName("UX_CounterShifts_Employee_Open");
        builder
            .HasIndex(x => new { x.EmployeeId, x.OpenedAt })
            .HasDatabaseName("IX_CounterShifts_Employee_OpenedAt")
            .IsDescending(false, true);
        builder.HasIndex(x => x.OpenedAt).HasDatabaseName("IX_CounterShifts_OpenedAt");
        builder
            .HasIndex(x => x.Status)
            .HasFilter("\"Status\" = 'Open'")
            .HasDatabaseName("IX_CounterShifts_Open");
    }
}
