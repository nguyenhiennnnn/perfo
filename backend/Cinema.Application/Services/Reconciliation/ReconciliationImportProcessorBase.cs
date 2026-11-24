using System.Globalization;
using System.Security.Cryptography;
using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Import;
using Cinema.Application.DTOs.Reconciliation;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Import;
using Cinema.Application.Utils;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.Reconciliation;

internal static class ReconciliationImportColumns
{
    public const string TransactionCode = "Mã giao dịch";
    public const string Amount = "Số tiền";
    public const string Fee = "Phí";
    public const string Status = "Trạng thái";
    public const string Time = "Thời gian";
}

internal sealed class ReconciliationImportContext(Dictionary<string, int> firstRowByCode)
    : IImportContext
{
    public Dictionary<string, int> FirstRowByCode { get; } = firstRowByCode;
}

public abstract class ReconciliationImportProcessorBase(
    IImportFileReader fileReader,
    IExportWriter exportWriter,
    IAppCache cache,
    IReconciliationService reconciliation,
    int userId
) : ImportProcessorBase<ReconciliationImportRow>(fileReader)
{
    public abstract ReconciliationGateway Gateway { get; }

    public override string Module => ImportExportModules.Reconciliation;

    public override string SheetName => Messages.Import_SheetReconciliation;

    protected abstract IReadOnlySet<string> SuccessStatuses { get; }

    protected abstract string[] TimeFormats { get; }

    protected abstract IReadOnlyList<IReadOnlyList<object?>> ExampleRows { get; }

    protected override IReadOnlyList<ImportColumn> Columns { get; } =
        [
            new(
                ReconciliationImportColumns.TransactionCode,
                true,
                Messages.Import_Reconciliation_Code_Desc,
                Messages.Import_Reconciliation_Code_Format
            ),
            new(
                ReconciliationImportColumns.Amount,
                true,
                Messages.Import_Reconciliation_Amount_Desc,
                Messages.Import_Reconciliation_Amount_Format
            ),
            new(
                ReconciliationImportColumns.Fee,
                false,
                Messages.Import_Reconciliation_Fee_Desc,
                Messages.Import_Reconciliation_Fee_Format
            ),
            new(
                ReconciliationImportColumns.Status,
                true,
                Messages.Import_Reconciliation_Status_Desc,
                Messages.Import_Reconciliation_Status_Format
            ),
            new(
                ReconciliationImportColumns.Time,
                true,
                Messages.Import_Reconciliation_Time_Desc,
                Messages.Import_Reconciliation_Time_Format
            ),
        ];

    protected override ReconciliationImportRow MapRow(IReadOnlyDictionary<string, string?> values) =>
        new(
            Gateway,
            values[ReconciliationImportColumns.TransactionCode],
            values[ReconciliationImportColumns.Amount],
            values[ReconciliationImportColumns.Fee],
            values[ReconciliationImportColumns.Status],
            values[ReconciliationImportColumns.Time]
        );

    public override async Task<ImportParseResult<ReconciliationImportRow>> ParseAsync(
        Stream content,
        ImportFileFormat format,
        CancellationToken cancellationToken
    )
    {
        var source = content;
        if (!source.CanSeek)
        {
            var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            source = copy;
        }

        source.Position = 0;
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(source, cancellationToken)).ToLowerInvariant();
        source.Position = 0;

        var parsed = await base.ParseAsync(source, format, cancellationToken);
        var rows = parsed
            .Rows.Select(r => r with { Data = r.Data with { FileHash = hash, CreatedByUserId = userId } })
            .ToList();
        return new ImportParseResult<ReconciliationImportRow>(parsed.Headers, rows);
    }

    public override async Task<Stream> BuildTemplateAsync(CancellationToken cancellationToken)
    {
        var guide = Columns
            .Select(c =>
                (IReadOnlyList<object?>)
                    new object?[]
                    {
                        c.Name,
                        c.Required ? Messages.Import_Required : Messages.Import_Optional,
                        c.Description,
                        c.Format,
                    }
            )
            .ToList();
        guide.Add(new object?[4]);
        guide.Add(
            [
                Messages.Import_GuideNote,
                null,
                string.Format(
                    Messages.Import_Note_Limits,
                    ImportLimits.MaxRows,
                    ImportLimits.MaxFileBytes / (1024 * 1024)
                ),
                null,
            ]
        );
        guide.Add([Messages.Import_GuideNote, null, Messages.Import_Reconciliation_Note, null]);

        var sheets = new List<ExportSheet>
        {
            new(
                SheetName,
                Columns.Select(c => c.Name).ToList(),
                ExampleRows,
                InputSheetOptions(ExampleRows.Count)
            ),
            new(
                Messages.Import_SheetGuide,
                [
                    Messages.Import_GuideColColumn,
                    Messages.Import_GuideColRequirement,
                    Messages.Import_GuideColDescription,
                    Messages.Import_GuideColFormat,
                ],
                guide,
                GuideSheetOptions
            ),
        };
        return await exportWriter.WriteXlsxWorkbookAsync(sheets, cancellationToken);
    }

    public override Task<IImportContext> CreateContextAsync(
        IReadOnlyList<ImportRow<ReconciliationImportRow>> rows,
        CancellationToken cancellationToken
    )
    {
        var first = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in rows)
            if (row.Data.TransactionCode is { } code)
                first.TryAdd(code, row.RowNumber);
        return Task.FromResult<IImportContext>(new ReconciliationImportContext(first));
    }

    public override ImportRow<ReconciliationImportRow> ValidateRow(
        ImportRow<ReconciliationImportRow> row,
        IImportContext context
    )
    {
        var lookups = (ReconciliationImportContext)context;
        var data = row.Data;
        var errors = new List<ImportCellError>();

        var code = data.TransactionCode;
        if (code is null)
            errors.Add(Required(ReconciliationImportColumns.TransactionCode));
        else if (code.Length > FieldLengths.TransactionCode)
            errors.Add(
                new(
                    ReconciliationImportColumns.TransactionCode,
                    string.Format(Messages.Invalid_Reconciliation_CodeTooLong, FieldLengths.TransactionCode)
                )
            );
        else if (lookups.FirstRowByCode.TryGetValue(code, out var firstRow) && firstRow != row.RowNumber)
            errors.Add(
                new(
                    ReconciliationImportColumns.TransactionCode,
                    string.Format(Messages.Invalid_Reconciliation_DuplicateInFile, firstRow)
                )
            );

        decimal amount = 0;
        if (data.Amount is null)
            errors.Add(Required(ReconciliationImportColumns.Amount));
        else if (!TryParseMoney(data.Amount, out amount))
            errors.Add(new(ReconciliationImportColumns.Amount, Messages.Invalid_Reconciliation_Amount));

        decimal fee = 0;
        if (data.Fee is not null && !TryParseMoney(data.Fee, out fee))
            errors.Add(new(ReconciliationImportColumns.Fee, Messages.Invalid_Reconciliation_Amount));

        if (data.Status is null)
            errors.Add(Required(ReconciliationImportColumns.Status));

        var timeUtc = DateTime.MinValue;
        if (data.Time is null)
            errors.Add(Required(ReconciliationImportColumns.Time));
        else if (!TryParseTime(data.Time, out timeUtc))
            errors.Add(new(ReconciliationImportColumns.Time, Messages.Invalid_Reconciliation_Time));

        GatewayStatementLine? line = null;
        if (errors.Count == 0)
            line = new GatewayStatementLine(
                code!,
                amount,
                fee,
                data.Status,
                SuccessStatuses.Contains(ImportHeaderNormalizer.Normalize(data.Status)),
                timeUtc
            );

        return row with { Errors = errors, Data = data with { Line = line } };
    }

    public override async Task<BulkActionResponse> ApplyRowsAsync(
        IReadOnlyList<ImportRow<ReconciliationImportRow>> rows,
        IJobProgressReporter? progress,
        CancellationToken cancellationToken
    )
    {
        var head = rows[0].Data;
        var lines = rows.Select(r => r.Data.Line!).ToList();
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var days = lines
            .Select(l => BusinessDay.Of(l.TimeUtc!.Value, settings.OpeningTime, settings.ClosingTime))
            .ToList();

        var result = await reconciliation.RunAsync(
            new ReconciliationRequest(
                head.Gateway,
                ReconciliationSource.File,
                null,
                head.FileHash,
                days.Min(),
                days.Max(),
                lines,
                head.CreatedByUserId
            ),
            cancellationToken
        );

        if (progress is not null)
            await progress.ReportAsync(rows.Count, cancellationToken);

        return BulkOperation.Build(
            rows.Select(r => new BulkActionResultItem(result.SessionId, true, null, r.RowNumber)).ToList()
        );
    }

    private static ImportCellError Required(string column) =>
        new(column, Messages.Invalid_ImportRequired);

    private bool TryParseTime(string text, out DateTime utc)
    {
        utc = default;
        if (
            !DateTime.TryParseExact(
                text,
                TimeFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed
            ) && !DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
        )
            return false;
        utc = BusinessTime.ToUtc(parsed);
        return true;
    }

    internal static bool TryParseMoney(string text, out decimal value)
    {
        value = 0;
        var t = new string(text.Where(c => char.IsDigit(c) || c is '.' or ',' or '-').ToArray());
        if (t.Length == 0 || t.Contains('-'))
            return false;

        var dots = t.Count(c => c == '.');
        var commas = t.Count(c => c == ',');
        char? decimalSep = null;
        if (dots > 0 && commas > 0)
            decimalSep = t[Math.Max(t.LastIndexOf('.'), t.LastIndexOf(','))];
        else if (dots + commas == 1)
        {
            var sep = dots == 1 ? '.' : ',';
            if (t.Length - t.IndexOf(sep) - 1 != 3)
                decimalSep = sep;
        }

        var normalized = new System.Text.StringBuilder();
        foreach (var c in t)
        {
            if (char.IsDigit(c))
                normalized.Append(c);
            else if (c == decimalSep)
                normalized.Append('.');
        }

        return decimal.TryParse(
            normalized.ToString(),
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value
        );
    }
}
