using System.Globalization;
using Cinema.Application.Caching;
using Cinema.Application.DTOs.Bundles;
using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Bundles;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Bundles;

public class BundleService(
    IAppDbContext db,
    IAppCache cache,
    IAuditLogger auditLogger,
    IBundleRetailPricer retailPricer
) : IBundleService
{
    public async Task<OffsetPageResult<BundleDto>> GetAdminAsync(
        int page,
        int pageSize,
        BundleListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.Bundles.AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(b => b.IsActive == isActive);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) => q.Where(b => EF.Functions.ILike(b.Name, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(b => b.Items)
            .ThenInclude(i => i.TicketType)
            .Include(b => b.Items)
            .ThenInclude(i => i.Combo)
            .OrderByDescending(b => b.BundleId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return OffsetPageResult<BundleDto>.Create(
            items.Select(BundleMapper.ToDto).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<BundleDto> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        BundleMapper.ToDto(
            await db
                .Bundles.AsNoTracking()
                .Include(b => b.Items)
                .ThenInclude(i => i.TicketType)
                .Include(b => b.Items)
                .ThenInclude(i => i.Combo)
                .AsSplitQuery()
                .FirstOrDefaultAsync(b => b.BundleId == id, cancellationToken)
                ?? throw new NotFoundException(Messages.NotFound_Bundle)
        );

    public async Task<BundleDto> CreateAsync(
        UpsertBundleRequest request,
        CancellationToken cancellationToken
    )
    {
        var name = request.Name.Trim();
        await EnsureValidAsync(0, name, request, cancellationToken);

        var entity = new Bundle { CreatedAt = DateTime.UtcNow };
        BundleMapper.Apply(entity, name, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Bundles.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.Bundle,
            entity.BundleId.ToString(),
            null,
            AuditSnapshots.BundleState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.Bundles, cancellationToken);
        return await GetByIdAsync(entity.BundleId, cancellationToken);
    }

    public async Task<BundleDto> UpdateAsync(
        int id,
        UpsertBundleRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        var name = request.Name.Trim();
        await EnsureValidAsync(id, name, request, cancellationToken);

        var before = AuditSnapshots.BundleState(entity);
        BundleMapper.Apply(entity, name, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.Bundle,
            entity.BundleId.ToString(),
            before,
            AuditSnapshots.BundleState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.Bundles, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<DeleteBundleResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);

        var before = AuditSnapshots.BundleState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.Bundle,
            entity.BundleId.ToString(),
            before,
            AuditSnapshots.BundleState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        await cache.RefreshAsync(CacheKeys.Bundles, cancellationToken);
        return new DeleteBundleResponse(id, true);
    }

    private async Task<Bundle> FindAsync(int id, CancellationToken cancellationToken) =>
        await db
            .Bundles.Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.BundleId == id, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_Bundle);

    private async Task EnsureValidAsync(
        int currentId,
        string name,
        UpsertBundleRequest request,
        CancellationToken cancellationToken
    )
    {
        var duplicated = await db.Bundles.AnyAsync(
            b => b.Name == name && b.BundleId != currentId,
            cancellationToken
        );
        if (duplicated)
            throw new ConflictAppException(Messages.Conflict_BundleNameExists);

        var input = BundleMapper.ToRetailInput(request);
        var errors = await BundleReferences.FindErrorsAsync(db, input, cancellationToken);
        if (errors.Count > 0)
            throw new ValidationAppException(Messages.Common_InvalidData, errors);

        var quote = await retailPricer.QuoteAsync(input, cancellationToken);
        if (quote is null)
            throw PriceError(Messages.Rule_Bundle_NoReferenceShowtime);
        if (request.Price >= quote.RetailTotal)
            throw PriceError(
                string.Format(Messages.Rule_Bundle_PriceNotBelowRetail, FormatMoney(quote.RetailTotal))
            );
    }

    private static ValidationAppException PriceError(string message) =>
        new(message, new Dictionary<string, string[]> { ["price"] = [message] });

    private static string FormatMoney(decimal amount) =>
        $"{amount.ToString("N0", new NumberFormatInfo { NumberGroupSeparator = "." })} {Messages.Common_CurrencyVnd}";
}
