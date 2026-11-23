using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public class InventoryStockQueryService(IAppDbContext db) : IInventoryStockQueryService
{
    public async Task<OffsetPageResult<StockLevelDto>> GetStockLevelsAsync(
        int page,
        int pageSize,
        StockLevelFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.InventoryItems.AsNoTracking().Where(i => i.IsActive);
        if (filter.LowStockOnly == true)
            query = query.Where(InventoryStockRules.IsLowStockExpression);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(i => EF.Functions.ILike(i.Name, pattern) || EF.Functions.ILike(i.Code, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(i => i.Name)
            .ThenBy(i => i.InventoryItemId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return OffsetPageResult<StockLevelDto>.Create(
            items.Select(InventoryMapper.ToStockLevel).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<StockLedgerResponse> GetLedgerAsync(
        int inventoryItemId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        if (fromDate is { } rangeFrom && toDate is { } rangeTo && rangeTo < rangeFrom)
            throw new ValidationAppException(
                Messages.Validation_To_BeforeFrom,
                new Dictionary<string, string[]>
                {
                    ["toDate"] = [Messages.Validation_To_BeforeFrom],
                }
            );

        var item =
            await db
                .InventoryItems.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(i => i.InventoryItemId == inventoryItemId)
                .Select(i => new
                {
                    i.InventoryItemId,
                    i.Code,
                    i.Name,
                    i.Unit,
                    i.QuantityOnHand,
                })
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_InventoryItem);

        var all = db.StockLedgers.AsNoTracking().Where(l => l.InventoryItemId == inventoryItemId);
        var range = all;
        var opening = 0m;
        if (fromDate is { } fromDay)
        {
            var fromUtc = BusinessTime.StartOfDayUtc(fromDay);
            range = range.Where(l => l.OccurredAt >= fromUtc);
            opening =
                await all.Where(l => l.OccurredAt < fromUtc)
                    .SumAsync(l => (decimal?)l.QuantityChange, cancellationToken) ?? 0m;
        }
        if (toDate is { } toDay)
        {
            var toUtc = BusinessTime.StartOfNextDayUtc(toDay);
            range = range.Where(l => l.OccurredAt < toUtc);
        }

        var totalIn =
            await range
                .Where(l => l.QuantityChange > 0)
                .SumAsync(l => (decimal?)l.QuantityChange, cancellationToken) ?? 0m;
        var totalOut =
            await range
                .Where(l => l.QuantityChange < 0)
                .SumAsync(l => (decimal?)(-l.QuantityChange), cancellationToken) ?? 0m;

        var total = await range.CountAsync(cancellationToken);
        var rows = await range
            .OrderBy(l => l.OccurredAt)
            .ThenBy(l => l.StockLedgerId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.StockLedgerId,
                l.OccurredAt,
                l.MovementType,
                l.QuantityChange,
                l.BalanceAfter,
                l.UnitCost,
                l.ReferenceType,
                l.ReferenceId,
                l.Reason,
                l.ActorUserId,
                ActorName = db
                    .Users.Where(u => u.UserId == l.ActorUserId)
                    .Select(u => u.FullName)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var entries = rows.Select(r => new StockLedgerEntryDto(
                r.StockLedgerId,
                r.OccurredAt,
                r.MovementType.ToString(),
                r.QuantityChange,
                r.BalanceAfter,
                r.UnitCost,
                r.ReferenceType,
                r.ReferenceId,
                r.Reason,
                r.ActorUserId,
                r.ActorName
            ))
            .ToList();

        return new StockLedgerResponse(
            item.InventoryItemId,
            item.Code,
            item.Name,
            item.Unit,
            item.QuantityOnHand,
            opening,
            totalIn,
            totalOut,
            opening + totalIn - totalOut,
            OffsetPageResult<StockLedgerEntryDto>.Create(entries, page, pageSize, total)
        );
    }

    public async Task<InventoryValuationDto> GetValuationAsync(CancellationToken cancellationToken)
    {
        var items = await db
            .InventoryItems.AsNoTracking()
            .Where(i => i.QuantityOnHand > 0)
            .ToListAsync(cancellationToken);

        var rows = items
            .Select(InventoryMapper.ToValuationItem)
            .OrderByDescending(r => r.StockValue)
            .ThenBy(r => r.Name)
            .ToList();

        return new InventoryValuationDto(DateTime.UtcNow, rows.Sum(r => r.StockValue), rows);
    }

    public async Task<LowStockAlertsResponse> GetAlertsAsync(CancellationToken cancellationToken)
    {
        var items = await db
            .InventoryItems.AsNoTracking()
            .Where(i => i.IsActive)
            .Where(InventoryStockRules.IsLowStockExpression)
            .OrderBy(i => i.QuantityOnHand - i.ReservedQuantity)
            .ThenBy(i => i.Name)
            .ThenBy(i => i.InventoryItemId)
            .ToListAsync(cancellationToken);
        if (items.Count == 0)
            return new LowStockAlertsResponse(0, []);

        var itemIds = items.Select(i => i.InventoryItemId).ToList();
        var comboRows = await db
            .ComboRecipeLines.AsNoTracking()
            .Where(l => itemIds.Contains(l.InventoryItemId) && l.Recipe.Combo.IsActive)
            .Select(l => new { l.InventoryItemId, ComboName = l.Recipe.Combo.Name })
            .ToListAsync(cancellationToken);
        var combosByItem = comboRows.ToLookup(r => r.InventoryItemId, r => r.ComboName);

        var alerts = items
            .Select(i =>
            {
                var available = i.QuantityOnHand - i.ReservedQuantity;
                return new LowStockAlertDto(
                    i.InventoryItemId,
                    i.Code,
                    i.Name,
                    i.Unit,
                    i.QuantityOnHand,
                    i.ReservedQuantity,
                    available,
                    i.LowStockThreshold,
                    available <= 0,
                    i.LowStockAlertedAt,
                    [.. combosByItem[i.InventoryItemId].Distinct().Order()]
                );
            })
            .ToList();

        return new LowStockAlertsResponse(alerts.Count, alerts);
    }
}
