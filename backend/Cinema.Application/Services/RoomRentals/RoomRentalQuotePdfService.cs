using System.Globalization;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Pdf;
using Cinema.Application.DTOs.RoomRentals;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.RoomRentals;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Shared.Resources;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.RoomRentals;

public class RoomRentalQuotePdfService(
    IAppDbContext db,
    IPdfGenerator pdfGenerator,
    IOptions<BrandOptions> brandOptions
) : IRoomRentalQuotePdfService
{
    private const string PdfContentType = "application/pdf";

    public async Task<ExportFileDto> ExportAsync(
        int requestId,
        int version,
        CancellationToken cancellationToken
    )
    {
        var quote = await RoomRentalQuoteStore.FindAsync(
            db,
            requestId,
            version,
            false,
            cancellationToken
        );
        var request = quote.Request!;
        var dto = RoomRentalQuoteStore.ToDto(quote, request.RequestCode, true);

        var settings = await db.LoadCinemaSettingsOrDefaultAsync(cancellationToken);
        var cinemaName = string.IsNullOrWhiteSpace(settings.Name)
            ? brandOptions.Value.Name
            : settings.Name;

        var keyValueColumns = new List<PdfTableColumn>
        {
            new(Messages.Quote_Pdf_ColItem, false, 1),
            new(Messages.Quote_Pdf_ColValue, false, 2),
        };

        var customer = new List<IReadOnlyList<string>>
        {
            Row(Messages.Quote_Pdf_Customer, request.FullName),
        };
        if (!string.IsNullOrWhiteSpace(request.OrganizationName))
            customer.Add(Row(Messages.Quote_Pdf_Organization, request.OrganizationName));
        customer.Add(Row(Messages.Quote_Pdf_Contact, $"{request.ContactPhone} - {request.ContactEmail}"));
        customer.Add(Row(Messages.Quote_Pdf_Room, dto.RoomName));
        customer.Add(Row(Messages.Quote_Pdf_StartAt, PdfCellFormatter.FormatDateTime(dto.StartAt)));
        customer.Add(
            Row(
                Messages.Quote_Pdf_Duration,
                string.Format(Messages.Quote_Pdf_DurationValue, dto.DurationMinutes)
            )
        );
        customer.Add(
            Row(
                Messages.Quote_Pdf_Attendees,
                dto.AttendeeCount.ToString(CultureInfo.InvariantCulture)
            )
        );

        var lineColumns = new List<PdfTableColumn>
        {
            new(Messages.Quote_Pdf_ColContent, false, 3),
            new(Messages.Quote_Pdf_ColQuantity, true, 1),
            new(Messages.Quote_Pdf_ColUnitPrice, true, 2),
            new(Messages.Quote_Pdf_ColAmount, true, 2),
        };
        var lineRows = dto
            .Lines.Select(l =>
                (IReadOnlyList<string>)
                    [
                        l.Label,
                        l.Quantity.ToString(CultureInfo.InvariantCulture),
                        Money(l.UnitPrice),
                        Money(l.Amount),
                    ]
            )
            .ToList();

        var payment = new List<IReadOnlyList<string>>
        {
            Row(
                string.Format(
                    Messages.Quote_Pdf_Deposit,
                    dto.DepositPercent.ToString("0.##", CultureInfo.InvariantCulture)
                ),
                MoneyWithUnit(dto.DepositAmount)
            ),
            Row(Messages.Quote_Pdf_Balance, MoneyWithUnit(dto.BalanceAmount)),
            Row(Messages.Quote_Pdf_BalanceDueAt, PdfCellFormatter.FormatDateTime(dto.BalanceDueAt)),
            Row(Messages.Quote_Pdf_ValidUntil, PdfCellFormatter.FormatDateTime(dto.ValidUntil)),
        };

        var cancellation = dto
            .CancellationTiers.Select(t =>
                Row(
                    string.Format(Messages.Quote_Pdf_CancelTier, FormatHours(t.MinHoursBeforeStart)),
                    string.Format(
                        Messages.Quote_Pdf_CancelRefund,
                        t.RefundPercent.ToString(CultureInfo.InvariantCulture)
                    )
                )
            )
            .ToList();
        cancellation.Add(Row(Messages.Quote_Pdf_CancelLate, Messages.Quote_Pdf_CancelNoRefund));

        var sections = new List<PdfReportSection>
        {
            new(
                Messages.Quote_Pdf_SectionInfo,
                keyValueColumns,
                customer,
                null,
                string.IsNullOrWhiteSpace(dto.Note) ? [] : [dto.Note]
            ),
            new(
                Messages.Quote_Pdf_SectionLines,
                lineColumns,
                lineRows,
                [Messages.Quote_Pdf_Total, string.Empty, string.Empty, Money(dto.Total)],
                []
            ),
            new(Messages.Quote_Pdf_SectionPayment, keyValueColumns, payment, null, []),
            new(Messages.Quote_Pdf_SectionCancellation, keyValueColumns, cancellation, null, []),
        };

        var document = new PdfSectionedReportDocument(
            Messages.Quote_Pdf_Title,
            cinemaName,
            string.Format(
                Messages.Quote_Pdf_Subtitle,
                request.RequestCode,
                dto.VersionNumber.ToString(CultureInfo.InvariantCulture)
            ),
            string.Format(
                Messages.Export_PdfExportedAt,
                PdfCellFormatter.FormatDateTime(DateTime.UtcNow)
            ),
            Messages.Export_PdfPage,
            Messages.Export_PdfNoData,
            sections
        );

        return new ExportFileDto(
            pdfGenerator.GenerateSectionedReport(document),
            PdfContentType,
            $"bao-gia-{request.RequestCode}-v{dto.VersionNumber}.pdf"
        );
    }

    private static IReadOnlyList<string> Row(string label, string value) => [label, value];

    private static string Money(decimal amount) =>
        PdfCellFormatter.Format(amount, ExportColumnType.Money);

    private static string MoneyWithUnit(decimal amount) =>
        $"{Money(amount)} {Messages.Common_CurrencyVnd}";

    private static string FormatHours(int hours) =>
        hours % 24 == 0
            ? string.Format(Messages.Quote_Pdf_Days, (hours / 24).ToString(CultureInfo.InvariantCulture))
            : string.Format(Messages.Quote_Pdf_Hours, hours.ToString(CultureInfo.InvariantCulture));
}
