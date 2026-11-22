using Cinema.Application.DTOs.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;

namespace Cinema.Application.Services.Audit;

internal static class AuditLogFilterExtensions
{
    public static IQueryable<AuditLog> ApplyFilter(
        this IQueryable<AuditLog> query,
        AuditLogListFilter filter
    )
    {
        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);
        if (!string.IsNullOrWhiteSpace(filter.EntityId))
            query = query.Where(a => a.EntityId == filter.EntityId);
        if (filter.ActorUserId is not null)
            query = query.Where(a => a.ActorUserId == filter.ActorUserId);
        if (filter.FromDate is not null)
            query = query.Where(a => a.OccurredAt >= DateTimeUtils.GetStartOfDay(filter.FromDate.Value));
        if (filter.ToDate is not null)
            query = query.Where(a =>
                a.OccurredAt < DateTimeUtils.GetStartOfNextDay(filter.ToDate.Value)
            );
        return query;
    }
}
