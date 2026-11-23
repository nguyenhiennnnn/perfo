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

public class WasteService(
    IAppDbContext db,
    IStockLedgerService stockLedger,
    IAuditLogger auditLogger,
    ICurrentActorAccessor actorAccessor
) : IWasteService
{
    public async Task<OffsetPageResult<WasteSheetListItemDto>> GetAdminAsync(
        int page,
        int pageSize,
        WasteSheetListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);
        InventorySheetSupport.EnsureDateRange(filter.FromDate, filter.ToDate);

        var query = db.WasteSheets.AsNoTracking().IgnoreQueryFilters();
        if (filter.FromDate is { } fromDate)
            query = query.Where(s => s.WasteDate >= fromDate);
        if (filter.ToDate is { } toDate)
            query = query.Where(s => s.WasteDate <= toDate);
        if (!string.IsNullOrWhiteSpace(filter.Reason))
        {
            var reason = EnumConverters.ParseEnum<WasteReason>(filter.Reason.Trim(), "reason");
            query = query.Where(s => s.Reason == reason);
        }
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
            .OrderByDescending(s => s.WasteDate)
            .ThenByDescending(s => s.WasteSheetId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<WasteSheetListItemDto>.Create(
            items.Select(s => InventoryMapper.ToListItem(s)).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<WasteSheetDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var sheet =
            await db
                .WasteSheets.AsNoTracking()
                .Include(s => s.Lines)
                .ThenInclude(l => l.InventoryItem)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.WasteSheetId == id, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_WasteSheet);

        var createdByName = await db.GetUserNameAsync(sheet.CreatedByUserId, cancellationToken);
        return InventoryMapper.ToDto(sheet, createdByName);
    }

    public async Task<WasteSheetDto> CreateAsync(
        CreateWasteSheetRequest request,
        CancellationToken cancellationToken
    )
    {
        var reason = EnumConverters.ParseEnum<WasteReason>(request.Reason.Trim(), "reason");
        var note = InventorySheetSupport.NormalizeNote(request.Note);
        var itemIds = request.Lines.Select(l => l.InventoryItemId).Distinct().Order().ToArray();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockInventoryItemsAsync(itemIds, cancellationToken);

        var states = await db
            .InventoryItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.InventoryItemId))
            .Select(i => new
            {
                i.InventoryItemId,
                i.QuantityOnHand,
                i.UnitCost,
            })
            .ToDictionaryAsync(i => i.InventoryItemId, cancellationToken);
        if (states.Count != itemIds.Length)
            throw new ValidationAppException(
                Messages.Invalid_WasteSheet_ItemNotFound,
                new Dictionary<string, string[]>
                {
                    ["lines"] = [Messages.Invalid_WasteSheet_ItemNotFound],
                }
            );

        if (request.Lines.Any(l => states[l.InventoryItemId].QuantityOnHand < l.Quantity))
            throw new BusinessRuleException(Messages.Rule_InventoryInsufficientStock);

        var sheet = new WasteSheet
        {
            WasteDate = request.WasteDate,
            Reason = reason,
            Note = note,
            CreatedByUserId = actorAccessor.Current?.UserId,
            CreatedAt = DateTime.UtcNow,
            Lines =
            [
                .. request.Lines.Select(l => new WasteSheetLine
                {
                    InventoryItemId = l.InventoryItemId,
                    Quantity = l.Quantity,
                    UnitCost = states[l.InventoryItemId].UnitCost,
                }),
            ],
        };
        db.WasteSheets.Add(sheet);
        await db.SaveChangesAsync(cancellationToken);

        var ledgerReason = InventorySheetSupport.WasteLedgerReason(reason, note);
        var quantityChanges = new List<StockQuantityChange>(sheet.Lines.Count);
        foreach (var line in sheet.Lines.OrderBy(l => l.InventoryItemId))
        {
            var posting = await stockLedger.PostAsync(
                new StockMovement(
                    line.InventoryItemId,
                    StockMovementType.Waste,
                    -line.Quantity,
                    StockReferenceTypes.Waste,
                    sheet.WasteSheetId,
                    ledgerReason
                ),
                cancellationToken
            );
            quantityChanges.Add(
                new StockQuantityChange(
                    line.InventoryItemId,
                    states[line.InventoryItemId].QuantityOnHand,
                    posting.BalanceAfter
                )
            );
        }

        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.WasteSheet,
            sheet.WasteSheetId.ToString(),
            null,
            AuditSnapshots.WasteSheetState(sheet, quantityChanges)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(sheet.WasteSheetId, cancellationToken);
    }
}
