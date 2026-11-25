using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Pdf;
using Cinema.Application.DTOs.Reports;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Reports;
using Cinema.Application.Options;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Reports.Finance;

public class FinanceReportExportService(
    IFinanceReportService financeReportService,
    IExportWriter exportWriter,
    IPdfGenerator pdfGenerator,
    IAppDbContext db,
    IOptions<BrandOptions> brandOptions
) : IFinanceReportExportService
{
    private const int MaxSheetNameLength = 31;
    private static readonly char[] InvalidSheetChars = ['\\', '/', '?', '*', '[', ']', ':'];

    public string Module => ImportExportModules.FinanceReport;

    public Task<int> CountAsync(FinanceExportFilter filter, CancellationToken cancellationToken)
    {
        var days = Math.Max(0, filter.To.DayNumber - filter.From.DayNumber + 1);
        return Task.FromResult(days * Math.Max(1, financeReportService.GetBlocks().Count));
    }

    public async Task<ExportFileDto> ExportAsync(
        FinanceExportFilter filter,
        ExportFormat format,
        ExportContext context,
        IJobProgressReporter? progress,
        CancellationToken cancellationToken
    )
    {
        if (format == ExportFormat.Csv)
            throw new ValidationAppException(
                Messages.Export_InvalidFinanceFormat,
                new Dictionary<string, string[]>
                {
                    ["format"] = [Messages.Export_InvalidFinanceFormat],
                }
            );

        var report = await financeReportService.GetAsync(
            filter.From,
            filter.To,
            null,
            cancellationToken
        );

        var content =
            format == ExportFormat.Pdf
                ? await WritePdfAsync(report, cancellationToken)
                : await exportWriter.WriteXlsxWorkbookAsync(
                    BuildSheets(report.Blocks),
                    cancellationToken
                );

        if (progress is not null)
            await progress.ReportAsync(report.Blocks.Sum(b => b.Rows.Count), cancellationToken);

        return new ExportFileDto(
            content,
            ExportFormats.ContentType(format),
            ExportFileName.Create(Module, ExportFormats.Extension(format))
        );
    }

    private static List<ExportSheet> BuildSheets(IReadOnlyList<ReportBlockResult> blocks)
    {
        var sheets = new List<ExportSheet>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var block in blocks)
        {
            var rows = new List<IReadOnlyList<object?>>();
            foreach (var row in block.Rows)
                rows.Add(Cells(block, row));
            if (block.Totals is not null)
                rows.Add(Cells(block, block.Totals));
            foreach (var warning in block.Warnings ?? [])
                rows.Add([warning]);

            sheets.Add(
                new ExportSheet(
                    UniqueName(block.Title, used),
                    block.Columns.Select(c => c.Title).ToList(),
                    rows
                )
            );
        }

        return sheets;
    }

    private static IReadOnlyList<object?> Cells(
        ReportBlockResult block,
        IReadOnlyDictionary<string, object?> row
    ) => block.Columns.Select(c => row.GetValueOrDefault(c.Key)).ToList();

    private static string UniqueName(string title, HashSet<string> used)
    {
        var cleaned = string.Concat(title.Where(ch => !InvalidSheetChars.Contains(ch))).Trim();
        if (cleaned.Length == 0)
            cleaned = Messages.Export_SheetRevenue;

        var baseName = cleaned.Length > MaxSheetNameLength
            ? cleaned[..MaxSheetNameLength]
            : cleaned;
        var name = baseName;
        for (var i = 2; !used.Add(name); i++)
        {
            var suffix = $" ({i})";
            name = baseName[..Math.Min(baseName.Length, MaxSheetNameLength - suffix.Length)] + suffix;
        }

        return name;
    }

    private async Task<Stream> WritePdfAsync(
        FinanceReportDto report,
        CancellationToken cancellationToken
    )
    {
        var settings = await db.LoadCinemaSettingsOrDefaultAsync(cancellationToken);
        var cinemaName = string.IsNullOrWhiteSpace(settings.Name)
            ? brandOptions.Value.Name
            : settings.Name;

        var sections = report
            .Blocks.Select(block =>
            {
                var columns = block
                    .Columns.Select(c =>
                    {
                        var numeric = c.Type != ReportColumnType.Text;
                        return new PdfTableColumn(c.Title, numeric, numeric ? 1 : 2);
                    })
                    .ToList();

                IReadOnlyList<string> Format(IReadOnlyDictionary<string, object?> row) =>
                    block.Columns.Select(c => FormatCell(c, row.GetValueOrDefault(c.Key))).ToList();

                return new PdfReportSection(
                    block.Title,
                    columns,
                    block.Rows.Select(Format).ToList(),
                    block.Totals is null ? null : Format(block.Totals),
                    block.Warnings ?? []
                );
            })
            .ToList();

        var document = new PdfSectionedReportDocument(
            Messages.Export_PdfTitleFinance,
            cinemaName,
            string.Format(
                Messages.Export_PdfPeriod,
                PdfCellFormatter.FormatDate(report.From),
                PdfCellFormatter.FormatDate(report.To)
            ),
            string.Format(
                Messages.Export_PdfExportedAt,
                PdfCellFormatter.FormatDateTime(DateTime.UtcNow)
            ),
            Messages.Export_PdfPage,
            Messages.Export_PdfNoData,
            sections
        );

        return pdfGenerator.GenerateSectionedReport(document);
    }

    private static string FormatCell(ReportColumn column, object? value)
    {
        if (value is null)
            return string.Empty;

        return column.Type switch
        {
            ReportColumnType.Money => PdfCellFormatter.Format(value, ExportColumnType.Money),
            ReportColumnType.Number => PdfCellFormatter.Format(value, ExportColumnType.Number),
            ReportColumnType.Percent => PdfCellFormatter.Format(value, ExportColumnType.Number) + "%",
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
                ?? string.Empty,
        };
    }
}
