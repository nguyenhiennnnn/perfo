using Cinema.Application.Caching;
using Cinema.Application.DTOs.Invoices;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Invoices;

public class InvoiceRequestService(IAppDbContext db, IAppCache cache, IFileStorage fileStorage)
    : IInvoiceRequestService
{
    public async Task<IReadOnlyList<InvoiceRequestDto>> GetForCustomerOrderAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order.UserId != userId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);

        var items = await db
            .InvoiceRequests.AsNoTracking()
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.InvoiceRequestId)
            .ToListAsync(cancellationToken);
        return items.Select(r => InvoiceMapper.ToDto(r, order.OrderCode)).ToList();
    }

    public async Task<InvoiceFileResult> DownloadForCustomerAsync(
        int userId,
        int orderId,
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var request =
            await db
                .InvoiceRequests.AsNoTracking()
                .Include(r => r.Order)
                .FirstOrDefaultAsync(
                    r => r.InvoiceRequestId == requestId && r.OrderId == orderId,
                    cancellationToken
                ) ?? throw new NotFoundException(Messages.NotFound_InvoiceRequest);
        if (request.Order.UserId != userId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);

        return await InvoiceFileReader.OpenAsync(fileStorage, request, cancellationToken);
    }

    public async Task<InvoiceRequestDto> CreateForCustomerAsync(
        int userId,
        int orderId,
        CreateInvoiceRequestRequest request,
        CancellationToken cancellationToken
    )
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order.UserId != userId)
            throw new ForbiddenAppException(Messages.Forbidden_NotBookingOwner);

        var profile =
            await db
                .InvoiceProfiles.AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.InvoiceProfileId == request.InvoiceProfileId && p.UserId == userId,
                    cancellationToken
                ) ?? throw new NotFoundException(Messages.NotFound_InvoiceProfile);

        await EnsureEligibleAsync(order, cancellationToken);

        var entity = new InvoiceRequest
        {
            OrderId = order.OrderId,
            InvoiceProfileId = profile.InvoiceProfileId,
            TaxCode = profile.TaxCode,
            CompanyName = profile.CompanyName,
            Address = profile.Address,
            Email = profile.Email,
            RequestedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
        };
        return await SaveAsync(entity, order.OrderCode, cancellationToken);
    }

    public async Task<InvoiceRequestDto> CreateForCounterAsync(
        int staffUserId,
        int orderId,
        CounterInvoiceRequestRequest request,
        CancellationToken cancellationToken
    )
    {
        var order = await LoadOrderAsync(orderId, cancellationToken);
        if (order.UserId is not null)
            throw new BusinessRuleException(Messages.Rule_InvoiceCounterWalkInOnly);

        await EnsureEligibleAsync(order, cancellationToken);

        var entity = new InvoiceRequest
        {
            OrderId = order.OrderId,
            TaxCode = request.TaxCode.Trim(),
            CompanyName = request.CompanyName.Trim(),
            Address = request.Address.Trim(),
            Email = request.Email.Trim(),
            RequestedByUserId = staffUserId,
            CreatedAt = DateTime.UtcNow,
        };
        return await SaveAsync(entity, order.OrderCode, cancellationToken);
    }

    private async Task<OrderInfo> LoadOrderAsync(int orderId, CancellationToken cancellationToken) =>
        await db
            .Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new OrderInfo(o.OrderId, o.OrderCode, o.UserId, o.Status, o.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_Order);

    private async Task EnsureEligibleAsync(OrderInfo order, CancellationToken cancellationToken)
    {
        if (order.Status != OrderStatus.Confirmed)
            throw new BusinessRuleException(Messages.Rule_InvoiceOrderNotPaid);

        var paidAt =
            await db
                .PaymentTransactions.Where(t =>
                    t.OrderId == order.OrderId
                    && t.Type == PaymentTransactionType.Charge
                    && t.Status == PaymentStatus.Success
                )
                .MaxAsync(t => (DateTime?)(t.CompletedAt ?? t.CreatedAt), cancellationToken)
            ?? order.CreatedAt;

        var settings = await cache.GetAsync(CacheKeys.CinemaSettings, cancellationToken);
        var lastDay = BusinessTime.LocalDate(paidAt).AddDays(settings.InvoiceRequestWindowDays);
        if (BusinessTime.LocalDate(DateTime.UtcNow) > lastDay)
            throw new BusinessRuleException(
                string.Format(Messages.Rule_InvoiceWindowExpired, settings.InvoiceRequestWindowDays)
            );

        var hasActive = await db.InvoiceRequests.AnyAsync(
            r => r.OrderId == order.OrderId && r.Status != InvoiceRequestStatus.Rejected,
            cancellationToken
        );
        if (hasActive)
            throw new ConflictAppException(Messages.Conflict_InvoiceRequestExists);
    }

    private async Task<InvoiceRequestDto> SaveAsync(
        InvoiceRequest entity,
        string orderCode,
        CancellationToken cancellationToken
    )
    {
        db.InvoiceRequests.Add(entity);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictAppException(Messages.Conflict_InvoiceRequestExists);
        }
        return InvoiceMapper.ToDto(entity, orderCode);
    }

    private sealed record OrderInfo(
        int OrderId,
        string OrderCode,
        int? UserId,
        OrderStatus Status,
        DateTime CreatedAt
    );
}
