using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class InvoiceProfileConfiguration : IEntityTypeConfiguration<InvoiceProfile>
{
    public void Configure(EntityTypeBuilder<InvoiceProfile> builder)
    {
        builder.ToTable("InvoiceProfiles");
        builder.HasKey(x => x.InvoiceProfileId);
        builder.Property(x => x.TaxCode).HasMaxLength(FieldLengths.TaxCode).IsRequired();
        builder
            .Property(x => x.CompanyName)
            .HasMaxLength(FieldLengths.InvoiceCompanyName)
            .IsRequired();
        builder.Property(x => x.Address).HasMaxLength(FieldLengths.InvoiceAddress).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(FieldLengths.Email).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp without time zone");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp without time zone");

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(x => new { x.UserId, x.TaxCode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE")
            .HasDatabaseName("UX_InvoiceProfiles_User_TaxCode");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
