using System.Globalization;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Pdf;
using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Orders;

public class OrderTicketPdfService(
    IAppDbContext db,
    IPdfGenerator pdfGenerator,
    IOptions<BrandOptions> brandOptions
) : IOrderTicketPdfService
{
    public Task<ExportFileDto> GenerateForCustomerAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken
    ) => BuildAsync(orderId, userId, cancellationToken);

    public Task<ExportFileDto> GenerateForStaffAsync(
        int orderId,
        CancellationToken cancellationToken
    ) => BuildAsync(orderId, null, cancellationToken);

    private async Task<ExportFileDto> BuildAsync(
        int orderId,
        int? ownerUserId,
        CancellationToken cancellationToken
    )
    {
        var order =
            await db
                .Orders.AsNoTracking()
                .Where(o => o.OrderId == orderId)
                .Select(o => new
                {
                    o.OrderCode,
                    o.UserId,
                    o.Status,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Order);

        if (ownerUserId is not null && order.UserId != ownerUserId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);

        if (order.Status != OrderStatus.Confirmed)
            throw new BusinessRuleException(Messages.Rule_TicketPdfNotPaid);

        var lines = await db
            .OrderLines.AsNoTracking()
            .Where(l =>
                l.OrderId == orderId
                && l.LineType == OrderLineType.Ticket
                && l.ShowtimeSeatId != null
                && l.RefundedQuantity < l.Quantity
            )
            .OrderBy(l => l.ShowtimeSeat!.Seat.SeatRow)
            .ThenBy(l => l.ShowtimeSeat!.Seat.SeatNumber)
            .Select(l => new
            {
                l.ShowtimeId,
                l.ShowtimeSeat!.Seat.SeatRow,
                l.ShowtimeSeat.Seat.SeatNumber,
                l.QrCode,
                TicketTypeName = l.TicketType != null ? l.TicketType.Name : null,
                l.PaidAmount,
                l.RequiresCardPresentation,
                MovieTitle = l.Showtime!.Movie.Title,
                l.Showtime.StartTime,
                l.Showtime.Room.RoomName,
            })
            .ToListAsync(cancellationToken);

        if (lines.Count == 0)
            throw new BusinessRuleException(Messages.Rule_TicketPdfNoTickets);

        var head = lines.OrderBy(l => l.StartTime).First();
        var tickets = lines.Where(l => l.ShowtimeId == head.ShowtimeId).ToList();

        var settings = await db.LoadCinemaSettingsOrDefaultAsync(cancellationToken);
        var cinemaName = string.IsNullOrWhiteSpace(settings.Name)
            ? brandOptions.Value.Name
            : settings.Name;

        var items = tickets
            .Select(t =>
            {
                var payload =
                    t.QrCode
                    ?? CodeGenerator.GenerateTicketQrPayload(
                        order.OrderCode,
                        t.SeatRow,
                        t.SeatNumber
                    );
                var typeName = t.TicketTypeName ?? Messages.Common_TicketTypeStandard;
                return new PdfTicketItem(
                    $"{t.SeatRow}{t.SeatNumber}",
                    QrCodeHelper.GeneratePngBytes(payload),
                    typeName,
                    FormatMoney(t.PaidAmount),
                    t.RequiresCardPresentation
                        ? string.Format(Messages.Export_PdfTicketCardNotice, typeName)
                        : null
                );
            })
            .ToList();

        var document = new PdfTicketDocument(
            cinemaName,
            head.MovieTitle,
            BusinessTime
                .ToLocal(head.StartTime)
                .ToString("HH:mm dd/MM/yyyy", CultureInfo.InvariantCulture),
            head.RoomName,
            order.OrderCode,
            new PdfTicketLabels(
                Messages.Export_PdfTicketTitle,
                Messages.Export_PdfTicketMovie,
                Messages.Export_PdfTicketShowtime,
                Messages.Export_PdfTicketRoom,
                Messages.Export_PdfTicketSeat,
                Messages.Export_PdfTicketBookingCode,
                Messages.Export_PdfTicketType,
                Messages.Export_PdfTicketPrice,
                Messages.Export_PdfTicketHint
            ),
            items
        );

        return new ExportFileDto(
            pdfGenerator.GenerateTickets(document),
            ExportFormats.ContentType(ExportFormat.Pdf),
            $"tickets-{order.OrderCode}.{ExportFormats.Extension(ExportFormat.Pdf)}"
        );
    }

    private static string FormatMoney(decimal amount) =>
        $"{amount.ToString("N0", CultureInfo.GetCultureInfo(RegionalDefaults.Locale))} {Messages.Common_CurrencyVnd}";
}
