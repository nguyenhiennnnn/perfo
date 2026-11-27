using System.Linq.Expressions;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Segments;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Segments;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Segments;

public class CustomerSegmentQueryService(IAppDbContext db, IAuditLogger auditLogger)
    : ICustomerSegmentQueryService
{
    private static readonly Expression<Func<CustomerSegmentProfile, bool>> CanReceiveMarketing =
        p => p.User.CustomerProfile != null && p.User.CustomerProfile.MarketingOptIn;

    public async Task<(int CustomerCount, int EligibleCount)> CountAsync(
        SegmentConditionNode conditions,
        CancellationToken cancellationToken
    )
    {
        var members = Members(conditions);
        var customers = await members.CountAsync(cancellationToken);
        var eligible = await members.Where(CanReceiveMarketing).CountAsync(cancellationToken);
        return (customers, eligible);
    }

    public async Task<OffsetPageResult<SegmentCustomerDto>> GetCustomersAsync(
        int segmentId,
        int page,
        int pageSize,
        SegmentCustomerListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var json =
            await db
                .CustomerSegments.AsNoTracking()
                .Where(s => s.SegmentId == segmentId)
                .Select(s => s.ConditionJson)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_CustomerSegment);

        var query = Members(SegmentConditionCompiler.Deserialize(json));
        if (filter.EligibleOnly)
            query = query.Where(CanReceiveMarketing);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) => q.Where(p => EF.Functions.ILike(p.User.FullName, pattern))
        );

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(p => p.TotalSpent)
            .ThenBy(p => p.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.UserId,
                p.User.FullName,
                p.User.Email,
                p.User.Phone,
                CanReceive = p.User.CustomerProfile != null && p.User.CustomerProfile.MarketingOptIn,
                p.ActivityStage,
                p.LastOrderAt,
                p.DaysSinceLastOrder,
                p.LookbackOrderCount,
                p.LookbackSpent,
                p.TotalSpent,
                p.EstimatedLifetimeValue,
                p.ExpiringPoints,
                p.IsNearTierUpgrade,
            })
            .ToListAsync(cancellationToken);

        if (filter.RevealPersonalData)
        {
            auditLogger.Record(
                AuditActions.RevealSegmentCustomers,
                AuditEntityTypes.CustomerSegment,
                segmentId.ToString(),
                null,
                new
                {
                    page,
                    pageSize,
                    userIds = rows.Select(r => r.UserId),
                }
            );
            await db.SaveChangesAsync(cancellationToken);
        }

        var mask = !filter.RevealPersonalData;
        return OffsetPageResult<SegmentCustomerDto>.Create(
            rows.Select(r => new SegmentCustomerDto(
                    r.UserId,
                    r.FullName,
                    mask ? PersonalDataMasker.MaskEmail(r.Email) : r.Email,
                    mask ? PersonalDataMasker.MaskPhone(r.Phone) : r.Phone,
                    mask,
                    r.CanReceive,
                    r.ActivityStage.ToString(),
                    r.LastOrderAt,
                    r.DaysSinceLastOrder,
                    r.LookbackOrderCount,
                    r.LookbackSpent,
                    r.TotalSpent,
                    r.EstimatedLifetimeValue,
                    r.ExpiringPoints,
                    r.IsNearTierUpgrade
                ))
                .ToList(),
            page,
            pageSize,
            total
        );
    }

    private IQueryable<CustomerSegmentProfile> Members(SegmentConditionNode conditions) =>
        db
            .CustomerSegmentProfiles.AsNoTracking()
            .Where(SegmentConditionCompiler.Compile(conditions))
            .Where(p => p.User.IsActive);
}
