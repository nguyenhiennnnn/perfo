using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Invoices;
using Cinema.Application.Interfaces;
using Cinema.Application.Services.Audit;
using Cinema.Application.Utils;
using Cinema.Domain.Entities;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Exceptions;
using Cinema.Shared.Resources;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Application.Services.Invoices;

public class InvoiceAdminService(
    IAppDbContext db,
    IFileStorage fileStorage,
    IAuditLogger auditLogger,
    ICurrentActorAccessor actorAccessor,
    INotificationService notificationService
) : IInvoiceAdminService
{
    public async Task<OffsetPageResult<InvoiceRequestDto>> GetAsync(
        int page,
        int pageSize,
        InvoiceRequestListFilter filter,
        CancellationToken cancellationToken
    )
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, Pagination.MaxPageSize);

        var query = db.InvoiceRequests.AsNoTracking();
        if (filter.Status is { } status)
            query = query.Where(r => r.Status == status);
        if (filter.NeedsAdjustment is { } needsAdjustment)
            query = query.Where(r => r.NeedsAdjustment == needsAdjustment);
        query = query.ApplySearch(
            filter.Search,
            (q, pattern) =>
                q.Where(r =>
                    EF.Functions.ILike(r.Order.OrderCode, pattern)
                    || EF.Functions.ILike(r.TaxCode, pattern)
                    || EF.Functions.ILike(r.CompanyName, pattern)
                    || (r.InvoiceNumber != null && EF.Functions.ILike(r.InvoiceNumber, pattern))
                )
        );

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.Status == InvoiceRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.InvoiceRequestId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { Request = r, r.Order.OrderCode })
            .ToListAsync(cancellationToken);

        return OffsetPageResult<InvoiceRequestDto>.Create(
            items.Select(x => InvoiceMapper.ToDto(x.Request, x.OrderCode)).ToList(),
            page,
            pageSize,
            total
        );
    }

    public async Task<InvoiceRequestDto> IssueAsync(
        int requestId,
        string invoiceNumber,
        InvoiceFileInput? file,
        CancellationToken cancellationToken
    )
    {
        invoiceNumber = (invoiceNumber ?? string.Empty).Trim();
        ValidateInvoiceNumber(invoiceNumber);
        if (file is not null)
            InvoiceFileRules.Ensure(file.FileName, file.Length);

        var request = await FindAsync(requestId, cancellationToken);
        if (request.Status != InvoiceRequestStatus.Pending)
            throw new ConflictAppException(Messages.Conflict_InvoiceRequestNotPending);

        FileUploadResult? uploaded = null;
        if (file is not null)
            uploaded = await UploadAsync(file, cancellationToken);

        var before = AuditSnapshots.InvoiceRequestState(request);
        var now = DateTime.UtcNow;
        var actorId = actorAccessor.Current?.UserId;
        var fileUrl = uploaded?.Url;
        var filePublicId = uploaded?.PublicId;
        var fileName = file?.FileName;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await db
            .InvoiceRequests.Where(r =>
                r.InvoiceRequestId == requestId && r.Status == InvoiceRequestStatus.Pending
            )
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(r => r.Status, InvoiceRequestStatus.Issued)
                        .SetProperty(r => r.InvoiceNumber, invoiceNumber)
                        .SetProperty(r => r.RejectReason, (string?)null)
                        .SetProperty(r => r.FileUrl, fileUrl)
                        .SetProperty(r => r.FilePublicId, filePublicId)
                        .SetProperty(r => r.FileName, fileName)
                        .SetProperty(r => r.IssuedByUserId, actorId)
                        .SetProperty(r => r.ProcessedAt, now),
                cancellationToken
            );
        if (affected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            await DeleteUploadedAsync(uploaded);
            throw new ConflictAppException(Messages.Conflict_InvoiceRequestNotPending);
        }

        db.InvoiceRequests.Entry(request).State = EntityState.Detached;
        var issued = await FindAsync(requestId, cancellationToken);
        auditLogger.Record(
            AuditActions.IssueInvoice,
            AuditEntityTypes.InvoiceRequest,
            requestId.ToString(),
            before,
            AuditSnapshots.InvoiceRequestState(issued)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        notificationService.SendInvoiceRequestProcessed(
            issued.Email,
            issued.CompanyName,
            issued.Order.OrderCode,
            InvoiceRequestStatus.Issued,
            issued.InvoiceNumber,
            null
        );
        return InvoiceMapper.ToDto(issued, issued.Order.OrderCode);
    }

    public async Task<InvoiceRequestDto> ReplaceFileAsync(
        int requestId,
        InvoiceFileInput file,
        CancellationToken cancellationToken
    )
    {
        InvoiceFileRules.Ensure(file.FileName, file.Length);
        var request = await FindAsync(requestId, cancellationToken);
        if (request.Status != InvoiceRequestStatus.Issued)
            throw new ConflictAppException(Messages.Conflict_InvoiceRequestNotIssued);

        var uploaded = await UploadAsync(file, cancellationToken);
        var oldPublicId = request.FilePublicId;
        var before = AuditSnapshots.InvoiceRequestState(request);

        request.FileUrl = uploaded.Url;
        request.FilePublicId = uploaded.PublicId;
        request.FileName = file.FileName;
        auditLogger.Record(
            AuditActions.ReplaceInvoiceFile,
            AuditEntityTypes.InvoiceRequest,
            requestId.ToString(),
            before,
            AuditSnapshots.InvoiceRequestState(request)
        );
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await DeleteUploadedAsync(uploaded);
            throw;
        }

        if (!string.IsNullOrEmpty(oldPublicId))
            await DeleteUploadedAsync(new FileUploadResult(string.Empty, oldPublicId));
        return InvoiceMapper.ToDto(request, request.Order.OrderCode);
    }

    public async Task<InvoiceRequestDto> RejectAsync(
        int requestId,
        RejectInvoiceRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(requestId, cancellationToken);
        if (entity.Status != InvoiceRequestStatus.Pending)
            throw new ConflictAppException(Messages.Conflict_InvoiceRequestNotPending);

        var before = AuditSnapshots.InvoiceRequestState(entity);
        var reason = request.Reason.Trim();
        var now = DateTime.UtcNow;
        var actorId = actorAccessor.Current?.UserId;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var affected = await db
            .InvoiceRequests.Where(r =>
                r.InvoiceRequestId == requestId && r.Status == InvoiceRequestStatus.Pending
            )
            .ExecuteUpdateAsync(
                s =>
                    s.SetProperty(r => r.Status, InvoiceRequestStatus.Rejected)
                        .SetProperty(r => r.RejectReason, reason)
                        .SetProperty(r => r.IssuedByUserId, actorId)
                        .SetProperty(r => r.ProcessedAt, now),
                cancellationToken
            );
        if (affected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictAppException(Messages.Conflict_InvoiceRequestNotPending);
        }

        db.InvoiceRequests.Entry(entity).State = EntityState.Detached;
        var rejected = await FindAsync(requestId, cancellationToken);
        auditLogger.Record(
            AuditActions.RejectInvoice,
            AuditEntityTypes.InvoiceRequest,
            requestId.ToString(),
            before,
            AuditSnapshots.InvoiceRequestState(rejected)
        );
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        notificationService.SendInvoiceRequestProcessed(
            rejected.Email,
            rejected.CompanyName,
            rejected.Order.OrderCode,
            InvoiceRequestStatus.Rejected,
            null,
            reason
        );
        return InvoiceMapper.ToDto(rejected, rejected.Order.OrderCode);
    }

    public async Task<InvoiceRequestDto> ResolveAdjustmentAsync(
        int requestId,
        ResolveInvoiceAdjustmentRequest request,
        CancellationToken cancellationToken
    )
    {
        var entity = await FindAsync(requestId, cancellationToken);
        if (!entity.NeedsAdjustment)
            throw new ConflictAppException(Messages.Conflict_InvoiceNoAdjustmentNeeded);

        var before = AuditSnapshots.InvoiceRequestState(entity);
        entity.NeedsAdjustment = false;
        entity.AdjustmentResolvedAt = DateTime.UtcNow;
        entity.AdjustmentResolvedByUserId = actorAccessor.Current?.UserId;
        entity.AdjustmentNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        auditLogger.Record(
            AuditActions.ResolveInvoiceAdjustment,
            AuditEntityTypes.InvoiceRequest,
            requestId.ToString(),
            before,
            AuditSnapshots.InvoiceRequestState(entity)
        );
        await db.SaveChangesAsync(cancellationToken);
        return InvoiceMapper.ToDto(entity, entity.Order.OrderCode);
    }

    public async Task<InvoiceFileResult> DownloadAsync(
        int requestId,
        CancellationToken cancellationToken
    )
    {
        var request = await FindAsync(requestId, cancellationToken);
        return await InvoiceFileReader.OpenAsync(fileStorage, request, cancellationToken);
    }

    private async Task<InvoiceRequest> FindAsync(int requestId, CancellationToken cancellationToken) =>
        await db
            .InvoiceRequests.Include(r => r.Order)
            .FirstOrDefaultAsync(r => r.InvoiceRequestId == requestId, cancellationToken)
        ?? throw new NotFoundException(Messages.NotFound_InvoiceRequest);

    private Task<FileUploadResult> UploadAsync(
        InvoiceFileInput file,
        CancellationToken cancellationToken
    ) =>
        fileStorage.UploadRawAsync(
            file.Content,
            file.FileName,
            InvoiceFileRules.ContentType,
            InvoiceFileRules.Folder,
            cancellationToken
        );

    private async Task DeleteUploadedAsync(FileUploadResult? uploaded)
    {
        if (uploaded is null || string.IsNullOrEmpty(uploaded.PublicId))
            return;
        try
        {
            await fileStorage.DeleteAsync(uploaded.PublicId);
        }
        catch { }
    }

    private static void ValidateInvoiceNumber(string invoiceNumber)
    {
        if (
            invoiceNumber.Length == 0
            || invoiceNumber.Length > FieldLengths.InvoiceNumber
            || NameNormalizer.ContainsDisallowedCharacters(invoiceNumber)
        )
            throw new ValidationAppException(Messages.Validation_Invoice_Number);
    }
}
