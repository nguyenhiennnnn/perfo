using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Inventory;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Inventory;

public class InventoryItemService(IAppDbContext db, IAuditLogger auditLogger) : IInventoryItemService
{
    public async Task<OffsetPageResult<InventoryItemDto>> GetAdminAsync(
        int page,
        int pageSize,
        InventoryItemListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.InventoryItems.AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(i => i.IsActive == isActive);
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

        return OffsetPageResult<InventoryItemDto>.Create(
            items.Select(InventoryMapper.ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<InventoryItemDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        InventoryMapper.ToDto(
            await db
                .InventoryItems.AsNoTracking()
                .FirstOrDefaultAsync(i => i.InventoryItemId == id, cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_InventoryItem)
        );

    public async Task<InventoryItemDto> CreateAsync(
        UpsertInventoryItemRequest request,
        CancellationToken cancellationToken
    )
    {
        var code = NormalizeCode(request.Code);
        await EnsureCodeAvailableAsync(0, code, cancellationToken);

        var entity = new InventoryItem { CreatedAt = DateTime.UtcNow };
        Apply(entity, code, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.InventoryItems.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.InventoryItem,
            entity.InventoryItemId.ToString(),
            null,
            AuditSnapshots.InventoryItemState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return InventoryMapper.ToDto(entity);
    }

    public async Task<InventoryItemDto> UpdateAsync(
        int id,
        UpsertInventoryItemRequest request,
        CancellationToken cancellationToken
    )
    {
        var code = NormalizeCode(request.Code);
        await EnsureCodeAvailableAsync(id, code, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockInventoryItemsAsync([id], cancellationToken);
        var entity = await FindAsync(id, cancellationToken);

        var unit = request.Unit.Trim();
        if (
            !string.Equals(entity.Unit, unit, StringComparison.Ordinal)
            && await db.StockLedgers.AnyAsync(l => l.InventoryItemId == id, cancellationToken)
        )
            throw new ConflictAppException(Messages.Conflict_InventoryItemUnitLocked);

        var before = AuditSnapshots.InventoryItemState(entity);
        Apply(entity, code, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.InventoryItem,
            entity.InventoryItemId.ToString(),
            before,
            AuditSnapshots.InventoryItemState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return InventoryMapper.ToDto(entity);
    }

    public async Task<DeleteInventoryItemResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockInventoryItemsAsync([id], cancellationToken);
        var entity = await FindAsync(id, cancellationToken);

        if (await db.ComboRecipeLines.AnyAsync(l => l.InventoryItemId == id, cancellationToken))
            throw new ConflictAppException(Messages.Conflict_InventoryItemInRecipe);
        if (entity.QuantityOnHand != 0)
            throw new ConflictAppException(Messages.Conflict_InventoryItemHasStock);

        var before = AuditSnapshots.InventoryItemState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.InventoryItem,
            entity.InventoryItemId.ToString(),
            before,
            AuditSnapshots.InventoryItemState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new DeleteInventoryItemResponse(id, true);
    }

    private async Task<InventoryItem> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.InventoryItems.FirstOrDefaultAsync(i => i.InventoryItemId == id, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_InventoryItem);

    private async Task EnsureCodeAvailableAsync(
        int currentId,
        string code,
        CancellationToken cancellationToken
    )
    {
        var duplicated = await db.InventoryItems.AnyAsync(
            i => i.Code == code && i.InventoryItemId != currentId,
            cancellationToken
        );
        if (duplicated)
            throw new ConflictAppException(Messages.Conflict_InventoryItemCodeExists);
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static void Apply(InventoryItem entity, string code, UpsertInventoryItemRequest request)
    {
        entity.Code = code;
        entity.Name = request.Name.Trim();
        entity.Unit = request.Unit.Trim();
        entity.LowStockThreshold = request.LowStockThreshold;
        entity.IsActive = request.IsActive;
    }
}
