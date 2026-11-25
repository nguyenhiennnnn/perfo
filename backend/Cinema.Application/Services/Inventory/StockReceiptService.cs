using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Auth;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Inventory;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public class StockReceiptService(
    IAppDbContext db,
    IStockLedgerService stockLedger,
    IAuditLogger auditLogger,
    ICurrentActorAccessor actorAccessor
) : IStockReceiptService
{
    public async Task<OffsetPageResult<StockReceiptListItemDto>> GetAdminAsync(
        int page,
        int pageSize,
        StockReceiptListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        if (filter.FromDate is { } rangeFrom && filter.ToDate is { } rangeTo && rangeTo < rangeFrom)
            throw new ValidationAppException(
                Messages.Validation_To_BeforeFrom,
                new Dictionary<string, string[]>
                {
                    ["toDate"] = [Messages.Validation_To_BeforeFrom],
                }
            );

        var query = db.StockReceipts.AsNoTracking();
        if (filter.FromDate is { } fromDate)
            query = query.Where(r => r.ReceivedDate >= fromDate);
        if (filter.ToDate is { } toDate)
            query = query.Where(r => r.ReceivedDate <= toDate);
        if (filter.InventoryItemId is { } itemId)
            query = query.Where(r => r.Lines.Any(l => l.InventoryItemId == itemId));
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) => q.Where(r => EF.Functions.ILike(r.SupplierName, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(r => r.Lines)
            .OrderByDescending(r => r.ReceivedDate)
            .ThenByDescending(r => r.StockReceiptId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<StockReceiptListItemDto>.Create(
            items.Select(InventoryMapper.ToListItem).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<StockReceiptDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var receipt =
            await db
                .StockReceipts.AsNoTracking()
                .Include(r => r.Lines)
                .ThenInclude(l => l.InventoryItem)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.StockReceiptId == id, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_StockReceipt);

        var createdByName = receipt.CreatedByUserId is { } userId
            ? await db
                .Users.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return InventoryMapper.ToDto(receipt, createdByName);
    }

    public async Task<StockReceiptDto> CreateAsync(
        CreateStockReceiptRequest request,
        CancellationToken cancellationToken
    )
    {
        var itemIds = request.Lines.Select(l => l.InventoryItemId).Distinct().Order().ToArray();
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockInventoryItemsAsync(itemIds, cancellationToken);

        var activeCount = await db.InventoryItems.CountAsync(
            i => itemIds.Contains(i.InventoryItemId) && i.IsActive,
            cancellationToken
        );
        if (activeCount != itemIds.Length)
            throw new ValidationAppException(
                Messages.Invalid_StockReceipt_ItemNotFound,
                new Dictionary<string, string[]>
                {
                    ["lines"] = [Messages.Invalid_StockReceipt_ItemNotFound],
                }
            );

        var receipt = new StockReceipt
        {
            SupplierName = request.SupplierName.Trim(),
            ReceivedDate = request.ReceivedDate,
            Note = note,
            CreatedByUserId = actorAccessor.Current?.UserId,
            CreatedAt = DateTime.UtcNow,
            Lines =
            [
                .. request.Lines.Select(l => new StockReceiptLine
                {
                    InventoryItemId = l.InventoryItemId,
                    Quantity = l.Quantity,
                    UnitCost = l.UnitCost,
                }),
            ],
        };
        db.StockReceipts.Add(receipt);
        await db.SaveChangesAsync(cancellationToken);

        var costChanges = new List<StockReceiptCostChange>(receipt.Lines.Count);
        foreach (var line in receipt.Lines.OrderBy(l => l.StockReceiptLineId))
        {
            var lineItemId = line.InventoryItemId;
            var current = await db
                .InventoryItems.AsNoTracking()
                .Where(i => i.InventoryItemId == lineItemId)
                .Select(i => new { i.QuantityOnHand, i.UnitCost })
                .FirstAsync(cancellationToken);

            var newCost = InventoryCosting.WeightedAverageCost(
                current.QuantityOnHand,
                current.UnitCost,
                line.Quantity,
                line.UnitCost
            );
            var posting = await stockLedger.PostAsync(
                new StockMovement(
                    lineItemId,
                    StockMovementType.Receipt,
                    line.Quantity,
                    StockReferenceTypes.StockReceipt,
                    receipt.StockReceiptId,
                    null,
                    newCost
                ),
                cancellationToken
            );
            costChanges.Add(
                new StockReceiptCostChange(
                    lineItemId,
                    current.QuantityOnHand,
                    current.UnitCost,
                    posting.UnitCost
                )
            );
        }

        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.StockReceipt,
            receipt.StockReceiptId.ToString(),
            null,
            AuditSnapshots.StockReceiptState(receipt, costChanges)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(receipt.StockReceiptId, cancellationToken);
    }
}
