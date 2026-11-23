using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public class StocktakeService(
    IAppDbContext db,
    IStockLedgerService stockLedger,
    IAuditLogger auditLogger,
    ICurrentActorAccessor actorAccessor
) : IStocktakeService
{
    public async Task<OffsetPageResult<StocktakeSheetListItemDto>> GetAdminAsync(
        int page,
        int pageSize,
        StocktakeSheetListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);
        InventorySheetSupport.EnsureDateRange(filter.FromDate, filter.ToDate);

        var query = db.StocktakeSheets.AsNoTracking().IgnoreQueryFilters();
        if (filter.FromDate is { } fromDate)
            query = query.Where(s => s.StocktakeDate >= fromDate);
        if (filter.ToDate is { } toDate)
            query = query.Where(s => s.StocktakeDate <= toDate);
        if (filter.InventoryItemId is { } itemId)
            query = query.Where(s => s.Lines.Any(l => l.InventoryItemId == itemId));
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(s =>
                    s.Lines.Any(l =>
                        EF.Functions.ILike(l.InventoryItem.Name, pattern)
                        || EF.Functions.ILike(l.InventoryItem.Code, pattern)
                    )
                )
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(s => s.Lines)
            .OrderByDescending(s => s.StocktakeDate)
            .ThenByDescending(s => s.StocktakeSheetId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<StocktakeSheetListItemDto>.Create(
            items.Select(s => InventoryMapper.ToListItem(s)).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<StocktakeSheetDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var sheet =
            await db
                .StocktakeSheets.AsNoTracking()
                .Include(s => s.Lines)
                .ThenInclude(l => l.InventoryItem)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.StocktakeSheetId == id, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_StocktakeSheet);

        var createdByName = await db.GetUserNameAsync(sheet.CreatedByUserId, cancellationToken);
        return InventoryMapper.ToDto(sheet, createdByName);
    }

    public async Task<StocktakeSheetDto> CreateAsync(
        CreateStocktakeSheetRequest request,
        CancellationToken cancellationToken
    )
    {
        var itemIds = request.Lines.Select(l => l.InventoryItemId).Distinct().Order().ToArray();
        var note = InventorySheetSupport.NormalizeNote(request.Note);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockInventoryItemsAsync(itemIds, cancellationToken);

        var bookQuantities = await db
            .InventoryItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.InventoryItemId))
            .Select(i => new { i.InventoryItemId, i.QuantityOnHand })
            .ToDictionaryAsync(i => i.InventoryItemId, i => i.QuantityOnHand, cancellationToken);
        if (bookQuantities.Count != itemIds.Length)
            throw new ValidationAppException(
                Messages.Invalid_StocktakeSheet_ItemNotFound,
                new Dictionary<string, string[]>
                {
                    ["lines"] = [Messages.Invalid_StocktakeSheet_ItemNotFound],
                }
            );

        var sheet = new StocktakeSheet
        {
            StocktakeDate = request.StocktakeDate,
            Note = note,
            CreatedByUserId = actorAccessor.Current?.UserId,
            CreatedAt = DateTime.UtcNow,
            Lines =
            [
                .. request.Lines.Select(l =>
                {
                    var book = bookQuantities[l.InventoryItemId];
                    return new StocktakeSheetLine
                    {
                        InventoryItemId = l.InventoryItemId,
                        BookQuantity = book,
                        CountedQuantity = l.CountedQuantity,
                        Difference = l.CountedQuantity - book,
                    };
                }),
            ],
        };
        db.StocktakeSheets.Add(sheet);
        await db.SaveChangesAsync(cancellationToken);

        var quantityChanges = new List<StockQuantityChange>(sheet.Lines.Count);
        foreach (var line in sheet.Lines.OrderBy(l => l.InventoryItemId))
        {
            var quantityAfter = line.CountedQuantity;
            if (line.Difference != 0)
            {
                var posting = await stockLedger.PostAsync(
                    new StockMovement(
                        line.InventoryItemId,
                        StockMovementType.StocktakeAdjustment,
                        line.Difference,
                        StockReferenceTypes.Stocktake,
                        sheet.StocktakeSheetId,
                        note
                    ),
                    cancellationToken
                );
                quantityAfter = posting.BalanceAfter;
            }

            quantityChanges.Add(
                new StockQuantityChange(line.InventoryItemId, line.BookQuantity, quantityAfter)
            );
        }

        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.StocktakeSheet,
            sheet.StocktakeSheetId.ToString(),
            null,
            AuditSnapshots.StocktakeSheetState(sheet, quantityChanges)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(sheet.StocktakeSheetId, cancellationToken);
    }
}
