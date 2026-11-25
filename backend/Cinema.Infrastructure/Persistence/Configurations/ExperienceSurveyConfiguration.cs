using Cinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cinema.Infrastructure.Persistence.Configurations;

public class ExperienceSurveyConfiguration : IEntityTypeConfiguration<ExperienceSurvey>
{
    public void Configure(EntityTypeBuilder<ExperienceSurvey> builder)
    {
        builder.ToTable(
            "ExperienceSurveys",
            t =>
            {
                t.HasCheckConstraint(
                    "CK_ExperienceSurveys_RoomRating",
                    "\"RoomRating\" BETWEEN 1 AND 5"
                );
                t.HasCheckConstraint(
                    "CK_ExperienceSurveys_SoundRating",
                    "\"SoundRating\" BETWEEN 1 AND 5"
                );
                t.HasCheckConstraint(
                    "CK_ExperienceSurveys_PictureRating",
                    "\"PictureRating\" BETWEEN 1 AND 5"
                );
                t.HasCheckConstraint(
                    "CK_ExperienceSurveys_CleanlinessRating",
                    "\"CleanlinessRating\" BETWEEN 1 AND 5"
                );
                t.HasCheckConstraint(
                    "CK_ExperienceSurveys_CounterServiceRating",
                    "\"CounterServiceRating\" BETWEEN 1 AND 5"
                );
            }
        );
        builder.HasKey(x => x.SurveyId);
        builder
            .Property(x => x.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("NOW()");

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Showtime)
            .WithMany()
            .HasForeignKey(x => x.ShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(x => new { x.UserId, x.ShowtimeId })
            .IsUnique()
            .HasDatabaseName("UQ_ExperienceSurveys_User_Showtime");
        builder.HasIndex(x => x.ShowtimeId).HasDatabaseName("IX_ExperienceSurveys_Showtime");
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_ExperienceSurveys_Order");
    }
}
