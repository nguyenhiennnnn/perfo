using Cinema.Application.Interfaces;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public static class InventorySheetSupport
{
    public static void EnsureDateRange(DateOnly? fromDate, DateOnly? toDate)
    {
        if (fromDate is { } from && toDate is { } to && to < from)
            throw new ValidationAppException(
                Messages.Validation_To_BeforeFrom,
                new Dictionary<string, string[]>
                {
                    ["toDate"] = [Messages.Validation_To_BeforeFrom],
                }
            );
    }

    public static string? NormalizeNote(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    public static string WasteLedgerReason(WasteReason reason, string? note)
    {
        var text = note is null ? reason.ToString() : $"{reason}: {note}";
        return text.Length <= FieldLengths.StockNote ? text : text[..FieldLengths.StockNote];
    }

    public static Task<string?> GetUserNameAsync(
        this IAppDbContext db,
        int? userId,
        CancellationToken cancellationToken
    ) =>
        userId is { } id
            ? db
                .Users.AsNoTracking()
                .Where(u => u.UserId == id)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken)
            : Task.FromResult<string?>(null);
}
