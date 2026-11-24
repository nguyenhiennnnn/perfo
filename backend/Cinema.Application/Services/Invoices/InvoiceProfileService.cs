using Cinema.Application.DTOs.Invoices;
using Cinema.Application.Interfaces;
using Cinema.Domain.Entities;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Invoices;

public class InvoiceProfileService(IAppDbContext db) : IInvoiceProfileService
{
    public async Task<IReadOnlyList<InvoiceProfileDto>> GetMineAsync(
        int userId,
        CancellationToken cancellationToken
    )
    {
        var items = await db
            .InvoiceProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.CompanyName)
            .ThenBy(p => p.InvoiceProfileId)
            .ToListAsync(cancellationToken);
        return items.Select(InvoiceMapper.ToDto).ToList();
    }

    public async Task<InvoiceProfileDto> CreateAsync(
        int userId,
        UpsertInvoiceProfileRequest request,
        CancellationToken cancellationToken
    )
    {
        var taxCode = request.TaxCode.Trim();
        await EnsureTaxCodeFreeAsync(userId, 0, taxCode, cancellationToken);

        var entity = new InvoiceProfile { UserId = userId, CreatedAt = DateTime.UtcNow };
        Apply(entity, taxCode, request);
        db.InvoiceProfiles.Add(entity);
        await SaveAsync(cancellationToken);
        return InvoiceMapper.ToDto(entity);
    }

    public async Task<InvoiceProfileDto> UpdateAsync(
        int userId,
        int profileId,
        UpsertInvoiceProfileRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindOwnedAsync(userId, profileId, cancellationToken);
        var taxCode = request.TaxCode.Trim();
        await EnsureTaxCodeFreeAsync(userId, profileId, taxCode, cancellationToken);

        Apply(entity, taxCode, request);
        entity.UpdatedAt = DateTime.UtcNow;
        await SaveAsync(cancellationToken);
        return InvoiceMapper.ToDto(entity);
    }

    public async Task<DeleteInvoiceProfileResponse> DeleteAsync(
        int userId,
        int profileId,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindOwnedAsync(userId, profileId, cancellationToken);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return new DeleteInvoiceProfileResponse(profileId, true);
    }

    private async Task<InvoiceProfile> FindOwnedAsync(
        int userId,
        int profileId,
        CancellationToken cancellationToken
    ) =>
        await db.InvoiceProfiles.FirstOrDefaultAsync(
            p => p.InvoiceProfileId == profileId && p.UserId == userId,
            cancellationToken
        ) ?? throw new NotFoundException(Messages.NotFound_InvoiceProfile);

    private async Task EnsureTaxCodeFreeAsync(
        int userId,
        int currentId,
        string taxCode,
        CancellationToken cancellationToken
    )
    {
        var exists = await db.InvoiceProfiles.AnyAsync(
            p => p.UserId == userId && p.TaxCode == taxCode && p.InvoiceProfileId != currentId,
            cancellationToken
        );
        if (exists)
            throw new ConflictAppException(Messages.Conflict_InvoiceProfileExists);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictAppException(Messages.Conflict_InvoiceProfileExists);
        }
    }

    private static void Apply(
        InvoiceProfile entity,
        string taxCode,
        UpsertInvoiceProfileRequest request
    )
    {
        entity.TaxCode = taxCode;
        entity.CompanyName = request.CompanyName.Trim();
        entity.Address = request.Address.Trim();
        entity.Email = request.Email.Trim();
    }
}
