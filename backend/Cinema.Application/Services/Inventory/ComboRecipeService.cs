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

public class ComboRecipeService(IAppDbContext db, IAuditLogger auditLogger) : IComboRecipeService
{
    public async Task<OffsetPageResult<ComboRecipeListItemDto>> GetAdminAsync(
        int page,
        int pageSize,
        ComboRecipeListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.ComboRecipes.AsNoTracking();
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) => q.Where(r => EF.Functions.ILike(r.Combo.Name, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.Combo.Name)
            .ThenBy(r => r.ComboId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ComboRecipeListItemDto(
                r.ComboId,
                r.Combo.Name,
                r.Lines.Count,
                r.UpdatedAt ?? r.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return OffsetPageResult<ComboRecipeListItemDto>.Create(items, page, pageSize, total);
    }

    public async Task<ComboRecipeDto> GetByComboIdAsync(
        int comboId,
        CancellationToken cancellationToken
    )
    {
        var comboName = await FindComboNameAsync(comboId, cancellationToken);
        var recipe =
            await db
                .ComboRecipes.AsNoTracking()
                .Include(r => r.Lines)
                .ThenInclude(l => l.InventoryItem)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.ComboId == comboId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_ComboRecipe);
        return InventoryMapper.ToDto(recipe, comboName);
    }

    public async Task<ComboRecipeDto> UpsertAsync(
        int comboId,
        UpsertComboRecipeRequest request,
        CancellationToken cancellationToken
    )
    {
        var comboName = await FindComboNameAsync(comboId, cancellationToken);
        var itemIds = request.Lines.Select(l => l.InventoryItemId).Distinct().Order().ToArray();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockInventoryItemsAsync(itemIds, cancellationToken);

        var activeCount = await db.InventoryItems.CountAsync(
            i => itemIds.Contains(i.InventoryItemId) && i.IsActive,
            cancellationToken
        );
        if (activeCount != itemIds.Length)
            throw new ValidationAppException(
                Messages.Invalid_ComboRecipe_ItemNotFound,
                new Dictionary<string, string[]>
                {
                    ["lines"] = [Messages.Invalid_ComboRecipe_ItemNotFound],
                }
            );

        var now = DateTime.UtcNow;
        var recipe = await db
            .ComboRecipes.Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.ComboId == comboId, cancellationToken);

        object? before = null;
        string action;
        if (recipe is null)
        {
            action = AuditActions.Create;
            recipe = new ComboRecipe { ComboId = comboId, CreatedAt = now };
            db.ComboRecipes.Add(recipe);
        }
        else
        {
            action = AuditActions.Update;
            before = AuditSnapshots.ComboRecipeState(comboId, recipe.Lines);
            recipe.UpdatedAt = now;
        }

        var requested = request.Lines.ToDictionary(l => l.InventoryItemId, l => l.Quantity);
        foreach (var line in recipe.Lines.Where(l => !requested.ContainsKey(l.InventoryItemId)).ToList())
            db.ComboRecipeLines.Remove(line);
        foreach (var line in recipe.Lines.Where(l => requested.ContainsKey(l.InventoryItemId)))
            line.Quantity = requested[line.InventoryItemId];

        var existing = recipe.Lines.Select(l => l.InventoryItemId).ToHashSet();
        foreach (var (itemId, quantity) in requested.Where(r => !existing.Contains(r.Key)))
            recipe.Lines.Add(new ComboRecipeLine { InventoryItemId = itemId, Quantity = quantity });

        var after = AuditSnapshots.ComboRecipeState(
            comboId,
            recipe.Lines.Where(l => requested.ContainsKey(l.InventoryItemId))
        );
        auditLogger.Record(action, AuditEntityTypes.ComboRecipe, comboId.ToString(), before, after);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByComboIdAsync(comboId, cancellationToken);
    }

    public async Task<DeleteComboRecipeResponse> DeleteAsync(
        int comboId,
        CancellationToken cancellationToken
    )
    {
        var recipe =
            await db
                .ComboRecipes.Include(r => r.Lines)
                .FirstOrDefaultAsync(r => r.ComboId == comboId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_ComboRecipe);

        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.ComboRecipe,
            comboId.ToString(),
            AuditSnapshots.ComboRecipeState(comboId, recipe.Lines),
            null
        );
        db.ComboRecipes.Remove(recipe);
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteComboRecipeResponse(comboId, true);
    }

    private async Task<string> FindComboNameAsync(int comboId, CancellationToken cancellationToken) =>
        await db
            .Combos.AsNoTracking()
            .Where(c => c.ComboId == comboId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_Combo);
}
