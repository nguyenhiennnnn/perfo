using System.Globalization;
using System.Text;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Shared.Constants;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;

namespace Cinema.Infrastructure.Export;

public class ExportWriter : IExportWriter
{
    private const double MaxColumnWidth = 60;
    private const int ExcelSheetNameMaxLength = 31;
    private const string IntegerFormat = "#,##0";
    private const string DecimalFormat = "#,##0.00";
    private const string MoneyFormat = "#,##0\" ₫\"";
    private const string DateFormat = DateTimePatterns.Date;
    private const string DateTimeFormat = DateTimePatterns.DateHourMinuteSecond;
    private const string HeaderFill = "#E5E7EB";
    private const string RequiredHeaderFill = "#FECACA";
    private const string ExampleFill = "#FEF9C3";
    private const string ExampleFont = "#6B7280";
    private const double FilterPadding = 3;
    private const int MaxInlineListLength = 255;

    public Task<Stream> WriteXlsxAsync<T>(
        string sheetName,
        IReadOnlyList<ExportColumn<T>> columns,
        IEnumerable<T> rows,
        IReadOnlyList<object?>? totalRow = null,
        CancellationToken cancellationToken = default
    ) => WriteXlsxAsync(sheetName, columns, ToAsyncEnumerable(rows), totalRow, cancellationToken);

    public async Task<Stream> WriteXlsxAsync<T>(
        string sheetName,
        IReadOnlyList<ExportColumn<T>> columns,
        IAsyncEnumerable<T> rows,
        IReadOnlyList<object?>? totalRow = null,
        CancellationToken cancellationToken = default
    )
    {
        using var workbook = new XLWorkbook();
        await AddSheetAsync(workbook, sheetName, columns, rows, totalRow, true, cancellationToken);
        return SaveWorkbook(workbook);
    }

    public async Task<Stream> WriteXlsxWorkbookAsync(
        IReadOnlyList<ExportSheet> sheets,
        CancellationToken cancellationToken = default
    )
    {
        using var workbook = new XLWorkbook();

        foreach (var exportSheet in sheets)
        {
            var columns = exportSheet
                .Headers.Select(
                    (header, index) =>
                        new ExportColumn<IReadOnlyList<object?>>(
                            header,
                            row => index < row.Count ? row[index] : null
                        )
                )
                .ToList();

            await AddSheetAsync<IReadOnlyList<object?>>(
                workbook,
                exportSheet.Name,
                columns,
                ToAsyncEnumerable(exportSheet.Rows),
                null,
                false,
                cancellationToken
            );
        }

        foreach (var exportSheet in sheets)
            ApplySheetOptions(workbook, exportSheet, sheets);

        return SaveWorkbook(workbook);
    }

    private static async Task AddSheetAsync<T>(
        XLWorkbook workbook,
        string sheetName,
        IReadOnlyList<ExportColumn<T>> columns,
        IAsyncEnumerable<T> rows,
        IReadOnlyList<object?>? totalRow,
        bool autoFilter,
        CancellationToken cancellationToken
    )
    {
        var sheet = workbook.Worksheets.Add(SanitizeSheetName(sheetName));
        var hasFraction = new bool[columns.Count];

        for (var c = 0; c < columns.Count; c++)
            sheet.Cell(1, c + 1).Value = XLCellValue.FromObject(
                ExportCellFormatter.ToSafeText(columns[c].Header)
            );

        var rowIndex = 1;
        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            rowIndex++;
            for (var c = 0; c < columns.Count; c++)
            {
                var value = columns[c].Selector(row);
                WriteCell(sheet.Cell(rowIndex, c + 1), columns[c].Type, value, hasFraction, c);
            }
        }

        var dataLastRow = rowIndex;

        if (totalRow is not null)
        {
            rowIndex++;
            for (var c = 0; c < columns.Count; c++)
            {
                var value = c < totalRow.Count ? totalRow[c] : null;
                WriteCell(sheet.Cell(rowIndex, c + 1), columns[c].Type, value, hasFraction, c);
            }

            sheet.Range(rowIndex, 1, rowIndex, columns.Count).Style.Font.Bold = true;
        }

        ApplyColumnFormats(sheet, columns, hasFraction, rowIndex);

        if (columns.Count > 0)
        {
            var header = sheet.Range(1, 1, 1, columns.Count);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderFill);
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            if (autoFilter && dataLastRow >= 2)
                sheet.Range(1, 1, dataLastRow, columns.Count).SetAutoFilter();
        }

        sheet.SheetView.FreezeRows(1);

        foreach (var column in sheet.ColumnsUsed())
        {
            column.AdjustToContents();
            var width = column.Width + (autoFilter ? FilterPadding : 0);
            column.Width = Math.Min(width, MaxColumnWidth);
        }
    }

    private static void ApplySheetOptions(
        XLWorkbook workbook,
        ExportSheet exportSheet,
        IReadOnlyList<ExportSheet> allSheets
    )
    {
        var options = exportSheet.Options;
        if (options is null || exportSheet.Headers.Count == 0)
            return;

        var sheet = workbook.Worksheet(SanitizeSheetName(exportSheet.Name));
        var headers = exportSheet.Headers;

        if (options.RequiredHeaders is { Count: > 0 } required)
        {
            for (var c = 0; c < headers.Count; c++)
            {
                if (required.Contains(headers[c]))
                    sheet.Cell(1, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(
                        RequiredHeaderFill
                    );
            }
        }

        var exampleCount = Math.Min(options.ExampleRowCount, exportSheet.Rows.Count);
        if (exampleCount > 0)
        {
            var examples = sheet.Range(2, 1, 1 + exampleCount, headers.Count);
            examples.Style.Font.Italic = true;
            examples.Style.Font.FontColor = XLColor.FromHtml(ExampleFont);
            examples.Style.Fill.BackgroundColor = XLColor.FromHtml(ExampleFill);
        }

        if (options.WrapText)
        {
            var used = sheet.RangeUsed();
            if (used is not null)
            {
                used.Style.Alignment.WrapText = true;
                used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }
        }

        if (options.Dropdowns is { Count: > 0 } dropdowns)
        {
            var lastRow = 1 + Math.Max(Math.Max(options.InputRowCapacity, exportSheet.Rows.Count), 1);
            foreach (var dropdown in dropdowns)
                ApplyDropdown(workbook, sheet, exportSheet, dropdown, allSheets, lastRow);
        }
    }

    private static void ApplyDropdown(
        XLWorkbook workbook,
        IXLWorksheet sheet,
        ExportSheet exportSheet,
        ExportDropdown dropdown,
        IReadOnlyList<ExportSheet> allSheets,
        int lastRow
    )
    {
        var column = IndexOfHeader(exportSheet.Headers, dropdown.Header);
        if (column < 0)
            return;

        string? inlineList = null;
        IXLRange? sourceRange = null;

        if (dropdown.Values is { Count: > 0 } values)
        {
            var list = string.Join(",", values);
            if (values.Any(v => v.Contains(',')) || list.Length > MaxInlineListLength)
                return;
            inlineList = list;
        }
        else if (dropdown.SourceSheet is not null && dropdown.SourceHeader is not null)
        {
            var source = allSheets.FirstOrDefault(s => s.Name == dropdown.SourceSheet);
            if (source is null)
                return;

            var sourceColumn = IndexOfHeader(source.Headers, dropdown.SourceHeader);
            if (sourceColumn < 0)
                return;

            var sourceLastRow = LastFilledRow(source, sourceColumn);
            if (sourceLastRow < 2)
                return;

            sourceRange = workbook
                .Worksheet(SanitizeSheetName(source.Name))
                .Range(2, sourceColumn + 1, sourceLastRow, sourceColumn + 1);
        }
        else
        {
            return;
        }

        var validation = sheet.Range(2, column + 1, lastRow, column + 1).CreateDataValidation();
        if (inlineList is not null)
            validation.List(inlineList, true);
        else
            validation.List(sourceRange!, true);

        validation.IgnoreBlanks = true;
        validation.ShowErrorMessage = true;
        validation.ErrorStyle = XLErrorStyle.Warning;
    }

    private static int IndexOfHeader(IReadOnlyList<string> headers, string header)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (string.Equals(headers[i], header, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private static int LastFilledRow(ExportSheet sheet, int columnIndex)
    {
        var last = 0;
        for (var i = 0; i < sheet.Rows.Count; i++)
        {
            var row = sheet.Rows[i];
            if (columnIndex < row.Count && row[columnIndex] is not null)
                last = i + 2;
        }

        return last;
    }

    private static Stream SaveWorkbook(XLWorkbook workbook)
    {
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    public Task<Stream> WriteCsvAsync<T>(
        IReadOnlyList<ExportColumn<T>> columns,
        IEnumerable<T> rows,
        CancellationToken cancellationToken = default
    ) => WriteCsvAsync(columns, ToAsyncEnumerable(rows), cancellationToken);

    public async Task<Stream> WriteCsvAsync<T>(
        IReadOnlyList<ExportColumn<T>> columns,
        IAsyncEnumerable<T> rows,
        CancellationToken cancellationToken = default
    )
    {
        var stream = new MemoryStream();
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            NewLine = "\r\n",
        };

        await using (var writer = new StreamWriter(stream, new UTF8Encoding(true), leaveOpen: true))
        await using (var csv = new CsvWriter(writer, configuration))
        {
            foreach (var column in columns)
                csv.WriteField(ExportCellFormatter.ToSafeText(column.Header));
            await csv.NextRecordAsync();

            await foreach (var row in rows.WithCancellation(cancellationToken))
            {
                foreach (var column in columns)
                    csv.WriteField(ToCsvField(column.Type, column.Selector(row)));
                await csv.NextRecordAsync();
            }

            await csv.FlushAsync();
        }

        stream.Position = 0;
        return stream;
    }

    private static void WriteCell(
        IXLCell cell,
        ExportColumnType type,
        object? value,
        bool[] hasFraction,
        int columnIndex
    )
    {
        if (value is null)
            return;

        switch (type)
        {
            case ExportColumnType.Number:
            case ExportColumnType.Money:
                var number = ExportCellFormatter.ToDecimal(value);
                if (number is null)
                    return;
                if (type == ExportColumnType.Number && ExportCellFormatter.HasFraction(number.Value))
                    hasFraction[columnIndex] = true;
                cell.Value = XLCellValue.FromObject(number.Value);
                break;

            case ExportColumnType.Date:
                var date = ExportCellFormatter.ToLocalDateTime(value);
                if (date is not null)
                    cell.Value = XLCellValue.FromObject(date.Value.Date);
                break;

            case ExportColumnType.DateTime:
                var dateTime = ExportCellFormatter.ToLocalDateTime(value);
                if (dateTime is not null)
                    cell.Value = XLCellValue.FromObject(dateTime.Value);
                break;

            default:
                var text = ExportCellFormatter.ToSafeText(value);
                if (text is not null)
                    cell.Value = XLCellValue.FromObject(text);
                break;
        }
    }

    private static void ApplyColumnFormats<T>(
        IXLWorksheet sheet,
        IReadOnlyList<ExportColumn<T>> columns,
        bool[] hasFraction,
        int lastRow
    )
    {
        if (lastRow < 2)
            return;

        for (var c = 0; c < columns.Count; c++)
        {
            var format = columns[c].Type switch
            {
                ExportColumnType.Number => hasFraction[c] ? DecimalFormat : IntegerFormat,
                ExportColumnType.Money => MoneyFormat,
                ExportColumnType.Date => DateFormat,
                ExportColumnType.DateTime => DateTimeFormat,
                _ => null,
            };

            if (format is not null)
                sheet.Range(2, c + 1, lastRow, c + 1).Style.NumberFormat.Format = format;
        }
    }

    private static string? ToCsvField(ExportColumnType type, object? value)
    {
        if (value is null)
            return null;

        switch (type)
        {
            case ExportColumnType.Number:
                var number = ExportCellFormatter.ToDecimal(value);
                return number is null ? null : ExportCellFormatter.FormatNumber(number.Value);

            case ExportColumnType.Money:
                var money = ExportCellFormatter.ToDecimal(value);
                return money is null ? null : ExportCellFormatter.FormatMoney(money.Value);

            case ExportColumnType.Date:
                var date = ExportCellFormatter.ToLocalDateTime(value);
                return date is null ? null : ExportCellFormatter.FormatDate(date.Value);

            case ExportColumnType.DateTime:
                var dateTime = ExportCellFormatter.ToLocalDateTime(value);
                return dateTime is null ? null : ExportCellFormatter.FormatDateTime(dateTime.Value);

            default:
                return ExportCellFormatter.ToSafeText(value);
        }
    }

    private static string SanitizeSheetName(string name)
    {
        var cleaned = new string(
            name.Where(ch => ch is not ('\\' or '/' or '?' or '*' or '[' or ']' or ':')).ToArray()
        );
        if (string.IsNullOrWhiteSpace(cleaned))
            cleaned = "Sheet1";
        return cleaned.Length > ExcelSheetNameMaxLength ? cleaned[..ExcelSheetNameMaxLength] : cleaned;
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
    {
        foreach (var item in source)
            yield return item;
        await Task.CompletedTask;
    }
}
