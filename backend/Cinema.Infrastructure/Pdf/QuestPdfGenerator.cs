using Cinema.Application.DTOs.Pdf;
using Cinema.Application.Interfaces;
using Cinema.Application.Options;
using Cinema.Shared.Resources;
using Microsoft.Extensions.Options;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cinema.Infrastructure.Pdf;

public class QuestPdfGenerator(IOptions<PdfOptions> options) : IPdfGenerator
{
    private static readonly object FontLock = new();
    private static volatile bool _fontsRegistered;

    private const float PageMargin = 36;
    private const float TicketMargin = 28;
    private const float QrSize = 170;

    private readonly PdfOptions _options = options.Value;

    public Stream GenerateReport(PdfReportDocument document)
    {
        EnsureFontsRegistered();

        var bytes = Document
            .Create(container =>
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(PageMargin);
                    page.DefaultTextStyle(style =>
                        style.FontFamily(_options.FontFamily).FontSize(10)
                    );

                    page.Header()
                        .PaddingBottom(12)
                        .Column(column =>
                        {
                            column.Item().Text(document.CinemaName).Bold().FontSize(11);
                            column.Item().PaddingTop(4).Text(document.Title).Bold().FontSize(18);
                            column
                                .Item()
                                .PaddingTop(2)
                                .Text(document.PeriodText)
                                .FontColor(Colors.Grey.Darken2);
                        });

                    page.Content().Element(content => ComposeReportBody(content, document));

                    page.Footer()
                        .DefaultTextStyle(style => style.FontSize(9).FontColor(Colors.Grey.Darken2))
                        .Row(row =>
                        {
                            row.RelativeItem().Text(document.ExportedAtText);
                            row.RelativeItem()
                                .AlignRight()
                                .Text(text =>
                                {
                                    text.Span($"{document.PageLabel} ");
                                    text.CurrentPageNumber();
                                    text.Span(" / ");
                                    text.TotalPages();
                                });
                        });
                })
            )
            .GeneratePdf();

        return new MemoryStream(bytes);
    }

    public Stream GenerateSectionedReport(PdfSectionedReportDocument document)
    {
        EnsureFontsRegistered();

        var bytes = Document
            .Create(container =>
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(PageMargin);
                    page.DefaultTextStyle(style =>
                        style.FontFamily(_options.FontFamily).FontSize(10)
                    );

                    page.Header()
                        .PaddingBottom(12)
                        .Column(column =>
                        {
                            column.Item().Text(document.CinemaName).Bold().FontSize(11);
                            column.Item().PaddingTop(4).Text(document.Title).Bold().FontSize(18);
                            column
                                .Item()
                                .PaddingTop(2)
                                .Text(document.PeriodText)
                                .FontColor(Colors.Grey.Darken2);
                        });

                    page.Content()
                        .Column(column =>
                        {
                            column.Spacing(18);
                            foreach (var section in document.Sections)
                            {
                                column
                                    .Item()
                                    .Column(item =>
                                    {
                                        item.Item().PaddingBottom(4).Text(section.Title).Bold().FontSize(13);
                                        foreach (var note in section.Notes)
                                            item.Item()
                                                .PaddingBottom(4)
                                                .Text(note)
                                                .FontSize(9)
                                                .FontColor(Colors.Orange.Darken3);

                                        if (section.Rows.Count == 0)
                                            item.Item()
                                                .Text(document.EmptyText)
                                                .FontColor(Colors.Grey.Darken2);
                                        else
                                            item.Item()
                                                .Element(c =>
                                                    ComposeTable(
                                                        c,
                                                        section.Columns,
                                                        section.Rows,
                                                        section.TotalRow
                                                    )
                                                );
                                    });
                            }
                        });

                    page.Footer()
                        .DefaultTextStyle(style => style.FontSize(9).FontColor(Colors.Grey.Darken2))
                        .Row(row =>
                        {
                            row.RelativeItem().Text(document.ExportedAtText);
                            row.RelativeItem()
                                .AlignRight()
                                .Text(text =>
                                {
                                    text.Span($"{document.PageLabel} ");
                                    text.CurrentPageNumber();
                                    text.Span(" / ");
                                    text.TotalPages();
                                });
                        });
                })
            )
            .GeneratePdf();

        return new MemoryStream(bytes);
    }

    public Stream GenerateTickets(PdfTicketDocument document)
    {
        EnsureFontsRegistered();

        var bytes = Document
            .Create(container =>
            {
                foreach (var ticket in document.Tickets)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A5);
                        page.Margin(TicketMargin);
                        page.DefaultTextStyle(style =>
                            style.FontFamily(_options.FontFamily).FontSize(11)
                        );

                        page.Content()
                            .Border(1)
                            .BorderColor(Colors.Grey.Darken2)
                            .Padding(20)
                            .Element(content => ComposeTicket(content, document, ticket));
                    });
                }
            })
            .GeneratePdf();

        return new MemoryStream(bytes);
    }

    private static void ComposeReportBody(IContainer container, PdfReportDocument document)
    {
        if (document.Rows.Count == 0)
        {
            container.Text(document.EmptyText).FontColor(Colors.Grey.Darken2);
            return;
        }

        ComposeTable(container, document.Columns, document.Rows, document.TotalRow);
    }

    private static void ComposeTable(
        IContainer container,
        IReadOnlyList<PdfTableColumn> columns,
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyList<string>? totalRow
    )
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(definition =>
            {
                foreach (var column in columns)
                    definition.RelativeColumn(column.Weight);
            });

            table.Header(header =>
            {
                foreach (var column in columns)
                {
                    var alignRight = column.AlignRight;
                    header
                        .Cell()
                        .Element(cell => HeaderCell(cell, alignRight))
                        .Text(column.Header)
                        .Bold();
                }
            });

            foreach (var row in rows)
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    var alignRight = columns[i].AlignRight;
                    var value = i < row.Count ? row[i] : string.Empty;
                    table.Cell().Element(cell => BodyCell(cell, alignRight)).Text(value);
                }
            }

            if (totalRow is { } total)
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    var alignRight = columns[i].AlignRight;
                    var value = i < total.Count ? total[i] : string.Empty;
                    table
                        .Cell()
                        .Element(cell => TotalCell(cell, alignRight))
                        .Text(value)
                        .Bold();
                }
            }
        });
    }

    private static IContainer HeaderCell(IContainer cell, bool alignRight) =>
        Align(
            cell.Background(Colors.Grey.Lighten3)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Darken1)
                .PaddingVertical(6)
                .PaddingHorizontal(4),
            alignRight
        );

    private static IContainer BodyCell(IContainer cell, bool alignRight) =>
        Align(
            cell.BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(5)
                .PaddingHorizontal(4),
            alignRight
        );

    private static IContainer TotalCell(IContainer cell, bool alignRight) =>
        Align(
            cell.BorderTop(1)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Darken1)
                .PaddingVertical(6)
                .PaddingHorizontal(4),
            alignRight
        );

    private static IContainer Align(IContainer cell, bool alignRight) =>
        alignRight ? cell.AlignRight() : cell.AlignLeft();

    private static void ComposeTicket(
        IContainer container,
        PdfTicketDocument document,
        PdfTicketItem ticket
    )
    {
        var labels = document.Labels;

        container.Column(column =>
        {
            column.Spacing(12);

            column.Item().Text(document.CinemaName).Bold().FontSize(12);
            column
                .Item()
                .Text(labels.Title)
                .FontSize(10)
                .FontColor(Colors.Grey.Darken2);
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            column.Item().Text(document.MovieTitle).Bold().FontSize(20);

            column
                .Item()
                .Row(row =>
                {
                    row.Spacing(12);
                    LabeledValue(row.RelativeItem(2), labels.Showtime, document.ShowtimeText);
                    LabeledValue(row.RelativeItem(1), labels.Room, document.RoomName);
                });

            column
                .Item()
                .Row(row =>
                {
                    row.Spacing(12);
                    LabeledValue(row.RelativeItem(1), labels.Seat, ticket.SeatLabel, 22);
                    LabeledValue(row.RelativeItem(2), labels.BookingCode, document.BookingCode);
                });

            column
                .Item()
                .Row(row =>
                {
                    row.Spacing(12);
                    LabeledValue(row.RelativeItem(2), labels.TicketType, ticket.TicketTypeName);
                    LabeledValue(row.RelativeItem(1), labels.Price, ticket.PriceText);
                });

            if (!string.IsNullOrEmpty(ticket.CardNotice))
                column.Item().Text(ticket.CardNotice).Bold().FontSize(10);

            column
                .Item()
                .PaddingTop(4)
                .AlignCenter()
                .Width(QrSize)
                .Height(QrSize)
                .Image(ticket.QrCodePng)
                .FitArea();

            column
                .Item()
                .AlignCenter()
                .Text(labels.Hint)
                .FontSize(9)
                .FontColor(Colors.Grey.Darken2);
        });
    }

    private static void LabeledValue(
        IContainer container,
        string label,
        string value,
        float valueSize = 12
    ) =>
        container.Column(column =>
        {
            column.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken2);
            column.Item().Text(value).Bold().FontSize(valueSize);
        });

    private void EnsureFontsRegistered()
    {
        if (_fontsRegistered)
            return;

        lock (FontLock)
        {
            if (_fontsRegistered)
                return;

            var directory = Path.IsPathRooted(_options.FontDirectory)
                ? _options.FontDirectory
                : Path.Combine(AppContext.BaseDirectory, _options.FontDirectory);

            foreach (var file in new[] { _options.RegularFontFile, _options.BoldFontFile })
            {
                var path = Path.Combine(directory, file);
                if (!File.Exists(path))
                    throw new InvalidOperationException(
                        string.Format(Messages.Config_PdfFontNotFound, path)
                    );

                using var stream = File.OpenRead(path);
                FontManager.RegisterFontFromStream(stream);
            }

            _fontsRegistered = true;
        }
    }
}
