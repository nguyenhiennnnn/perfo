using Cinema.Application.DTOs.Surveys;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Surveys;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Surveys;

public class ExperienceSurveyService(IAppDbContext db) : IExperienceSurveyService
{
    public async Task<ExperienceSurveyDto> SubmitAsync(
        int userId,
        int showtimeId,
        SubmitExperienceSurveyRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!await db.Showtimes.AnyAsync(s => s.ShowtimeId == showtimeId, cancellationToken))
            throw new NotFoundException(Messages.NotFound_Showtime);

        var orderId = await db
            .OrderLines.AsNoTracking()
            .ExcludingExchangedOut(db)
            .Where(l =>
                l.LineType == OrderLineType.Ticket
                && l.IsCheckedIn
                && l.ShowtimeId == showtimeId
                && l.RefundedQuantity < l.Quantity
                && l.Order.UserId == userId
                && l.Order.Status == OrderStatus.Confirmed
            )
            .OrderBy(l => l.CheckedInAt)
            .ThenBy(l => l.OrderLineId)
            .Select(l => (int?)l.OrderId)
            .FirstOrDefaultAsync(cancellationToken);
        if (orderId is not { } checkedInOrderId)
            throw new BusinessRuleException(Messages.Rule_SurveyRequiresCheckIn);

        if (await ExistsAsync(userId, showtimeId, cancellationToken))
            throw new ConflictAppException(Messages.Conflict_SurveyAlreadyExists);

        var survey = new ExperienceSurvey
        {
            UserId = userId,
            ShowtimeId = showtimeId,
            OrderId = checkedInOrderId,
            RoomRating = request.RoomRating,
            SoundRating = request.SoundRating,
            PictureRating = request.PictureRating,
            CleanlinessRating = request.CleanlinessRating,
            CounterServiceRating = request.CounterServiceRating,
            CreatedAt = DateTime.UtcNow,
        };
        db.ExperienceSurveys.Add(survey);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ExperienceSurveys.Remove(survey);
            if (await ExistsAsync(userId, showtimeId, CancellationToken.None))
                throw new ConflictAppException(Messages.Conflict_SurveyAlreadyExists);
            throw;
        }

        return ToDto(survey);
    }

    public async Task<ExperienceSurveyDto> GetMineAsync(
        int userId,
        int showtimeId,
        CancellationToken cancellationToken
    )
    {
        var survey =
            await db
                .ExperienceSurveys.AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.UserId == userId && s.ShowtimeId == showtimeId,
                    cancellationToken
                ) ?? throw new NotFoundException(Messages.NotFound_ExperienceSurvey);
        return ToDto(survey);
    }

    private Task<bool> ExistsAsync(int userId, int showtimeId, CancellationToken cancellationToken) =>
        db.ExperienceSurveys.AnyAsync(
            s => s.UserId == userId && s.ShowtimeId == showtimeId,
            cancellationToken
        );

    private static ExperienceSurveyDto ToDto(ExperienceSurvey s) =>
        new(
            s.SurveyId,
            s.ShowtimeId,
            s.OrderId,
            s.RoomRating,
            s.SoundRating,
            s.PictureRating,
            s.CleanlinessRating,
            s.CounterServiceRating,
            s.CreatedAt
        );
}
