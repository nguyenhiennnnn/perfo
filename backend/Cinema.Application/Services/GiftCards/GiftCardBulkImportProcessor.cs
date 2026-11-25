using System.Globalization;
using System.Net.Mail;
using Cinema.Application.Caching;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.DTOs.Import;
using Cinema.Application.Interfaces.Import;
using Cinema.Application.Interfaces.ImportExport;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Services.Import;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;

namespace Cinema.Application.Services.GiftCards;

internal static class GiftCardBulkImportColumns
{
    public const string RecipientName = nameof(RecipientName);
    public const string RecipientEmail = nameof(RecipientEmail);
    public const string Amount = nameof(Amount);
    public const string Message = nameof(Message);
}

public class GiftCardBulkImportProcessor(
    IImportFileReader fileReader,
    IAppCache cache,
    IExportWriter exportWriter
) : ImportProcessorBase<GiftCardBulkImportRow>(fileReader)
{
    private static readonly Dictionary<string, string>[] ExampleData =
    [
        new()
        {
            [GiftCardBulkImportColumns.RecipientName] = "Nguyễn Văn A",
            [GiftCardBulkImportColumns.RecipientEmail] = "nguyen.van.a@example.com",
            [GiftCardBulkImportColumns.Amount] = "200000",
            [GiftCardBulkImportColumns.Message] = "Chúc mừng sinh nhật!",
        },
        new()
        {
            [GiftCardBulkImportColumns.RecipientName] = "Trần Thị B",
            [GiftCardBulkImportColumns.RecipientEmail] = "tran.thi.b@example.com",
            [GiftCardBulkImportColumns.Amount] = "500000",
        },
    ];

    public override string Module => ImportExportModules.GiftCardBulk;

    public override string SheetName => Messages.Import_SheetGiftCards;

    protected override IReadOnlyList<ImportColumn> Columns { get; } =
        [
            new(
                GiftCardBulkImportColumns.RecipientName,
                true,
                Messages.Import_GiftCard_RecipientName_Desc,
                Messages.Import_GiftCard_RecipientName_Format
            ),
            new(
                GiftCardBulkImportColumns.RecipientEmail,
                true,
                Messages.Import_GiftCard_RecipientEmail_Desc,
                Messages.Import_GiftCard_RecipientEmail_Format
            ),
            new(
                GiftCardBulkImportColumns.Amount,
                true,
                Messages.Import_GiftCard_Amount_Desc,
                Messages.Import_GiftCard_Amount_Format
            ),
            new(
                GiftCardBulkImportColumns.Message,
                false,
                Messages.Import_GiftCard_Message_Desc,
                Messages.Import_GiftCard_Message_Format
            ),
        ];

    protected override GiftCardBulkImportRow MapRow(IReadOnlyDictionary<string, string?> values) =>
        new(
            values[GiftCardBulkImportColumns.RecipientName],
            values[GiftCardBulkImportColumns.RecipientEmail],
            values[GiftCardBulkImportColumns.Amount],
            values[GiftCardBulkImportColumns.Message]
        );

    public override async Task<Stream> BuildTemplateAsync(CancellationToken cancellationToken)
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var sheets = new List<ExportSheet>
        {
            new(
                SheetName,
                Columns.Select(c => c.Name).ToList(),
                ExampleRows(),
                InputSheetOptions(ExampleData.Length)
            ),
            new(
                Messages.Import_SheetGuide,
                [
                    Messages.Import_GuideColColumn,
                    Messages.Import_GuideColRequirement,
                    Messages.Import_GuideColDescription,
                    Messages.Import_GuideColFormat,
                ],
                GuideRows(settings),
                GuideSheetOptions
            ),
        };

        return await exportWriter.WriteXlsxWorkbookAsync(sheets, cancellationToken);
    }

    public override async Task<IImportContext> CreateContextAsync(
        IReadOnlyList<ImportRow<GiftCardBulkImportRow>> rows,
        CancellationToken cancellationToken
    )
    {
        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);

        var firstRowByEmail = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in rows.OrderBy(r => r.RowNumber))
            if (row.Data.RecipientEmail is { } email)
                firstRowByEmail.TryAdd(NormalizeEmail(email), row.RowNumber);

        return new GiftCardBulkImportContext(
            settings.GiftCardMinAmount,
            settings.GiftCardMaxAmount,
            firstRowByEmail
        );
    }

    public override ImportRow<GiftCardBulkImportRow> ValidateRow(
        ImportRow<GiftCardBulkImportRow> row,
        IImportContext context
    )
    {
        var limits = (GiftCardBulkImportContext)context;
        var data = row.Data;
        var errors = new List<ImportCellError>();

        if (data.RecipientName is null)
            errors.Add(Required(GiftCardBulkImportColumns.RecipientName));
        else if (data.RecipientName.Length > FieldLengths.PersonName)
            errors.Add(
                new ImportCellError(
                    GiftCardBulkImportColumns.RecipientName,
                    string.Format(Messages.Invalid_ImportTextTooLong, FieldLengths.PersonName)
                )
            );
        else if (HasControlCharacters(data.RecipientName))
            errors.Add(
                new ImportCellError(
                    GiftCardBulkImportColumns.RecipientName,
                    Messages.Invalid_ImportControlCharacters
                )
            );

        if (data.RecipientEmail is null)
            errors.Add(Required(GiftCardBulkImportColumns.RecipientEmail));
        else if (data.RecipientEmail.Length > FieldLengths.Email)
            errors.Add(
                new ImportCellError(
                    GiftCardBulkImportColumns.RecipientEmail,
                    string.Format(Messages.Invalid_ImportTextTooLong, FieldLengths.Email)
                )
            );
        else if (!IsValidEmail(data.RecipientEmail))
            errors.Add(
                new ImportCellError(
                    GiftCardBulkImportColumns.RecipientEmail,
                    Messages.Validation_Email_Invalid
                )
            );
        else if (
            limits.FirstRowByEmail.TryGetValue(NormalizeEmail(data.RecipientEmail), out var first)
            && first != row.RowNumber
        )
            errors.Add(
                new ImportCellError(
                    GiftCardBulkImportColumns.RecipientEmail,
                    string.Format(Messages.Invalid_ImportGiftCardDuplicateEmail, first)
                )
            );

        decimal? faceValue = null;
        if (data.Amount is null)
            errors.Add(Required(GiftCardBulkImportColumns.Amount));
        else if (
            !decimal.TryParse(
                data.Amount,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount
            )
            || amount != decimal.Truncate(amount)
        )
            errors.Add(
                new ImportCellError(
                    GiftCardBulkImportColumns.Amount,
                    Messages.Invalid_ImportInteger
                )
            );
        else if (amount < limits.MinAmount || amount > limits.MaxAmount)
            errors.Add(
                new ImportCellError(
                    GiftCardBulkImportColumns.Amount,
                    string.Format(
                        Messages.Rule_GiftCardAmountOutOfRange,
                        GiftCardRules.FormatMoney(limits.MinAmount),
                        GiftCardRules.FormatMoney(limits.MaxAmount)
                    )
                )
            );
        else
            faceValue = amount;

        if (data.Message is not null)
        {
            if (data.Message.Length > FieldLengths.GiftCardMessage)
                errors.Add(
                    new ImportCellError(
                        GiftCardBulkImportColumns.Message,
                        string.Format(
                            Messages.Invalid_ImportTextTooLong,
                            FieldLengths.GiftCardMessage
                        )
                    )
                );
            else if (HasControlCharacters(data.Message))
                errors.Add(
                    new ImportCellError(
                        GiftCardBulkImportColumns.Message,
                        Messages.Invalid_ImportControlCharacters
                    )
                );
        }

        return row with
        {
            Data = data with { FaceValue = errors.Count == 0 ? faceValue : null },
            Errors = errors,
        };
    }

    public override Task<BulkActionResponse> ApplyRowsAsync(
        IReadOnlyList<ImportRow<GiftCardBulkImportRow>> rows,
        IJobProgressReporter? progress,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    private static ImportCellError Required(string column) =>
        new(column, Messages.Invalid_ImportRequired);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool HasControlCharacters(string value) => value.Any(char.IsControl);

    private static bool IsValidEmail(string value) =>
        MailAddress.TryCreate(value, out var address)
        && string.Equals(address.Address, value, StringComparison.Ordinal)
        && address.Host.Contains('.');

    private IReadOnlyList<IReadOnlyList<object?>> ExampleRows() =>
        ExampleData
            .Select(example =>
                (IReadOnlyList<object?>)
                    Columns.Select(c => (object?)example.GetValueOrDefault(c.Name)).ToArray()
            )
            .ToList();

    private IReadOnlyList<IReadOnlyList<object?>> GuideRows(CinemaSettingsSnapshot settings)
    {
        var rows = Columns
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

        rows.Add(new object?[4]);
        rows.Add(
            GuideNote(
                string.Format(
                    Messages.Import_Note_Limits,
                    ImportLimits.MaxRows,
                    ImportLimits.MaxFileBytes / (1024 * 1024)
                )
            )
        );
        rows.Add(
            GuideNote(
                string.Format(
                    Messages.Import_Note_GiftCard_Rules,
                    GiftCardRules.FormatMoney(settings.GiftCardMinAmount),
                    GiftCardRules.FormatMoney(settings.GiftCardMaxAmount)
                )
            )
        );
        rows.Add(GuideNote(Messages.Import_Note_RequiredColumns));
        rows.Add(GuideNote(Messages.Import_Note_GiftCard_Examples));

        return rows;
    }

    private static object?[] GuideNote(string text) =>
        [Messages.Import_GuideNote, null, text, null];

    private sealed class GiftCardBulkImportContext(
        decimal minAmount,
        decimal maxAmount,
        Dictionary<string, int> firstRowByEmail
    ) : IImportContext
    {
        public decimal MinAmount { get; } = minAmount;
        public decimal MaxAmount { get; } = maxAmount;
        public Dictionary<string, int> FirstRowByEmail { get; } = firstRowByEmail;
    }
}
