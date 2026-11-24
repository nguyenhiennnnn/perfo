namespace Cinema.Application.DTOs.Pdf;

public record PdfTableColumn(string Header, bool AlignRight = false, float Weight = 1);

public record PdfReportDocument(
    string Title,
    string CinemaName,
    string PeriodText,
    string ExportedAtText,
    string PageLabel,
    string EmptyText,
    IReadOnlyList<PdfTableColumn> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<string>? TotalRow
);

public record PdfReportSection(
    string Title,
    IReadOnlyList<PdfTableColumn> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<string>? TotalRow,
    IReadOnlyList<string> Notes
);

public record PdfSectionedReportDocument(
    string Title,
    string CinemaName,
    string PeriodText,
    string ExportedAtText,
    string PageLabel,
    string EmptyText,
    IReadOnlyList<PdfReportSection> Sections
);

public record PdfTicketLabels(
    string Title,
    string Movie,
    string Showtime,
    string Room,
    string Seat,
    string BookingCode,
    string TicketType,
    string Price,
    string Hint
);

public record PdfTicketItem(
    string SeatLabel,
    byte[] QrCodePng,
    string TicketTypeName,
    string PriceText,
    string? CardNotice
);

public record PdfTicketDocument(
    string CinemaName,
    string MovieTitle,
    string ShowtimeText,
    string RoomName,
    string BookingCode,
    PdfTicketLabels Labels,
    IReadOnlyList<PdfTicketItem> Tickets
);
