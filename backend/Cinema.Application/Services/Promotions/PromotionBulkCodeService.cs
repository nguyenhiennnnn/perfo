using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Interfaces.Audit;
using Cinema.Application.Interfaces.Infrastructure;
using Cinema.Application.Interfaces.Promotions;
using Cinema.Application.Options;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cinema.Application.Services.Promotions;

public class PromotionBulkCodeService(
    IAppDbContext db,
    IAuditLogger auditLogger,
    IOptions<PromotionOptions> options
) : IPromotionBulkCodeService
{
    public async Task<BulkPersonalCodesResponse> CreateAsync(
        BulkPersonalCodesRequest request,
        CancellationToken cancellationToken
    )
    {
        var prefix = PromotionMapper.NormalizeCode(request.CodePrefix);
        var userIds = request.UserIds.Distinct().ToList();

        var users = await db
            .Users.AsNoTracking()
            .Where(u =>
                userIds.Contains(u.UserId) && u.IsActive && u.Role.RoleName == Roles.Customer
            )
            .Select(u => new { u.UserId, u.FullName, u.Email })
            .ToListAsync(cancellationToken);

        var errors = await PromotionReferences.FindErrorsAsync(
            db,
            request.MovieIds,
            request.ShowtimeIds,
            request.ScreenTypeIds,
            request.MembershipTierIds,
            cancellationToken
        );
        if (users.Count != userIds.Count)
            errors["userIds"] = [Messages.Invalid_Promotion_BulkUsersNotFound];
        if (errors.Count > 0)
            throw new ValidationAppException(Messages.Common_InvalidData, errors);

        var endsAt = ResolveEndsAt(request);
        var codes = await GenerateCodesAsync(prefix, userIds.Count, cancellationToken);

        var now = DateTime.UtcNow;
        var entities = new List<Promotion>(users.Count);
        for (var i = 0; i < users.Count; i++)
        {
            var entity = new Promotion { CreatedAt = now };
            PromotionMapper.Apply(
                entity,
                codes[i],
                PromotionBulkMapper.ToUpsert(request, codes[i], users[i].UserId, endsAt)
            );
            entities.Add(entity);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Promotions.AddRange(entities);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var entity in entities)
            auditLogger.Record(
                AuditActions.Create,
                AuditEntityTypes.Promotion,
                entity.PromotionId.ToString(),
                null,
                AuditSnapshots.PromotionState(entity)
            );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var items = entities
            .Select(e =>
            {
                var user = users.First(u => u.UserId == e.AssignedUserId);
                return new BulkPersonalCodeItemDto(
                    e.PromotionId,
                    e.Code,
                    user.UserId,
                    user.FullName,
                    user.Email,
                    e.EndsAt
                );
            })
            .ToList();
        return new BulkPersonalCodesResponse(items.Count, items);
    }

    private DateTime? ResolveEndsAt(BulkPersonalCodesRequest request)
    {
        if (request.EndsAt is { } endsAt)
            return endsAt;

        var validDays = request.ValidDays ?? options.Value.PersonalCodeValidDays;
        var start = request.StartsAt is { } startsAt
            ? BusinessTime.ToUtc(startsAt)
            : DateTime.UtcNow;
        return start.AddDays(validDays);
    }

    private async Task<List<string>> GenerateCodesAsync(
        string prefix,
        int count,
        CancellationToken cancellationToken
    )
    {
        var codes = new HashSet<string>();
        for (var attempt = 0; attempt < PromotionLimits.GenerateCodeMaxAttempts; attempt++)
        {
            while (codes.Count < count)
                codes.Add($"{prefix}-{ReferralCodeGenerator.Generate()}");

            var candidates = codes.ToList();
            var taken = await db
                .Promotions.AsNoTracking()
                .Where(p => candidates.Contains(p.Code))
                .Select(p => p.Code)
                .ToListAsync(cancellationToken);
            if (taken.Count == 0)
                return candidates;

            codes.ExceptWith(taken);
        }

        throw new ConflictAppException(Messages.Conflict_PromotionCodeExists);
    }
}
