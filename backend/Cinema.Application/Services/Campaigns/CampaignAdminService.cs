using Cinema.Application.DTOs.Campaigns;
using Cinema.Application.DTOs.Common;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Campaigns;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Campaigns;

public class CampaignAdminService(
    IAppDbContext db,
    IAuditLogger auditLogger,
    ICampaignAudienceService audienceService
) : ICampaignAdminService
{
    public async Task<OffsetPageResult<CampaignDto>> GetAsync(
        int page,
        int pageSize,
        CampaignListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.Campaigns.AsNoTracking();
        if (
            !string.IsNullOrWhiteSpace(filter.Status)
            && Enum.TryParse<CampaignStatus>(filter.Status, true, out var status)
        )
            query = query.Where(c => c.Status == status);
        if (filter.SegmentId is { } segmentId)
            query = query.Where(c => c.SegmentId == segmentId);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(c =>
                    EF.Functions.ILike(c.Name, pattern) || EF.Functions.ILike(c.Subject, pattern)
                )
        );

        var total = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.CampaignId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var names = await LoadSegmentNamesAsync(
            entities.Select(c => c.SegmentId).Distinct().ToList(),
            cancellationToken
        );
        return OffsetPageResult<CampaignDto>.Create(
            entities.Select(c => ToDto(c, names.GetValueOrDefault(c.SegmentId, string.Empty), null)).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<CampaignDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var entity =
            await db
                .Campaigns.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CampaignId == id, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_Campaign);
        return await ToDetailAsync(entity, cancellationToken);
    }

    public async Task<CampaignDto> CreateAsync(
        UpsertCampaignRequest request,
        CancellationToken cancellationToken
    )
    {
        await EnsureSegmentAsync(request, cancellationToken);

        var entity = new Campaign { CreatedAt = DateTime.UtcNow };
        Apply(entity, request);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Campaigns.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        auditLogger.Record(
            AuditActions.Create,
            AuditEntityTypes.Campaign,
            entity.CampaignId.ToString(),
            null,
            AuditSnapshots.CampaignState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await ToDetailAsync(entity, cancellationToken);
    }

    public async Task<CampaignDto> UpdateAsync(
        int id,
        UpsertCampaignRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity.Status is not (CampaignStatus.Draft or CampaignStatus.Scheduled))
            throw new ConflictAppException(Messages.Rule_Campaign_NotEditable);
        await EnsureSegmentAsync(request, cancellationToken);

        var before = AuditSnapshots.CampaignState(entity);
        Apply(entity, request);
        entity.UpdatedAt = DateTime.UtcNow;
        auditLogger.Record(
            AuditActions.Update,
            AuditEntityTypes.Campaign,
            entity.CampaignId.ToString(),
            before,
            AuditSnapshots.CampaignState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        return await ToDetailAsync(entity, cancellationToken);
    }

    public async Task<DeleteCampaignResponse> DeleteAsync(
        int id,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(id, cancellationToken);
        if (entity.Status == CampaignStatus.Sending)
            throw new ConflictAppException(Messages.Rule_Campaign_NotDeletable);

        var before = AuditSnapshots.CampaignState(entity);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
        auditLogger.Record(
            AuditActions.Delete,
            AuditEntityTypes.Campaign,
            entity.CampaignId.ToString(),
            before,
            AuditSnapshots.CampaignState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);

        return new DeleteCampaignResponse(id, true);
    }

    private async Task<Campaign> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Campaigns.FirstOrDefaultAsync(c => c.CampaignId == id, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_Campaign);

    private async Task EnsureSegmentAsync(
        UpsertCampaignRequest request,
        CancellationToken cancellationToken
    )
    {
        var isActive =
            await db
                .CustomerSegments.AsNoTracking()
                .Where(s => s.SegmentId == request.SegmentId)
                .Select(s => (bool?)s.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound_CustomerSegment);
        if (request.ScheduledAt is not null && !isActive)
            throw new BusinessRuleException(Messages.Rule_Campaign_SegmentInactive);
    }

    private static void Apply(Campaign entity, UpsertCampaignRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.SegmentId = request.SegmentId;
        entity.Channel = Enum.Parse<CampaignChannel>(request.Channel, true);
        entity.Subject = request.Subject.Trim();
        entity.Body = request.Body.Trim();

        var promo = request.Promo;
        PromotionDiscountType? type = promo is null
            ? null
            : Enum.Parse<PromotionDiscountType>(promo.DiscountType, true);
        entity.PromoDiscountType = type;
        entity.PromoDiscountValue = promo?.DiscountValue;
        entity.PromoMaxDiscountAmount =
            type == PromotionDiscountType.Percent ? promo!.MaxDiscountAmount : null;
        entity.PromoValidityDays = promo?.ValidityDays;

        entity.ScheduledAt = request.ScheduledAt is { } scheduledAt
            ? BusinessTime.ToUtc(scheduledAt)
            : null;
        entity.Status = entity.ScheduledAt is null ? CampaignStatus.Draft : CampaignStatus.Scheduled;
    }

    private async Task<Dictionary<int, string>> LoadSegmentNamesAsync(
        List<int> segmentIds,
        CancellationToken cancellationToken
    ) =>
        await db
            .CustomerSegments.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => segmentIds.Contains(s.SegmentId))
            .ToDictionaryAsync(s => s.SegmentId, s => s.Name, cancellationToken);

    private async Task<CampaignDto> ToDetailAsync(
        Campaign entity,
        CancellationToken cancellationToken
    )
    {
        var names = await LoadSegmentNamesAsync([entity.SegmentId], cancellationToken);
        var audience = await audienceService.SummarizeAsync(entity, cancellationToken);
        return ToDto(entity, names.GetValueOrDefault(entity.SegmentId, string.Empty), audience);
    }

    private static CampaignDto ToDto(
        Campaign entity,
        string segmentName,
        CampaignAudienceDto? audience
    ) =>
        new(
            entity.CampaignId,
            entity.Name,
            entity.SegmentId,
            segmentName,
            entity.Channel.ToString(),
            entity.Subject,
            entity.Body,
            entity.PromoDiscountType is { } type
                ? new CampaignPromoDto(
                    type.ToString(),
                    entity.PromoDiscountValue!.Value,
                    entity.PromoMaxDiscountAmount,
                    entity.PromoValidityDays!.Value
                )
                : null,
            entity.ScheduledAt,
            entity.Status.ToString(),
            audience,
            entity.CreatedAt,
            entity.UpdatedAt
        );
}
