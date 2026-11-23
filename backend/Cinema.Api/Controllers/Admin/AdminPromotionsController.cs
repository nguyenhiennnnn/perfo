using Cinema.Api.Extensions;
using Cinema.Api.Filters;
using Cinema.Application.DTOs.Promotions;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/promotions")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminPromotionsController(
    IPromotionService promotionService,
    IPromotionReportService promotionReportService,
    IPromotionRedemptionService promotionRedemptionService,
    IPromotionBulkCodeService promotionBulkCodeService,
    IExportJobService exportJobService
) : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] bool? isActive,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await promotionService.GetAdminAsync(
                page,
                pageSize,
                new PromotionListFilter(search, type, isActive),
                cancellationToken
            )
        );

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports(
        [FromQuery] PromotionReportFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(await promotionReportService.GetListAsync(filter, page, pageSize, cancellationToken));

    [HttpGet("{id:int}/report")]
    public async Task<IActionResult> GetReport(
        int id,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken cancellationToken
    ) => Ok(await promotionReportService.GetAsync(id, fromDate, toDate, cancellationToken));

    [HttpGet("{id:int}/redemptions")]
    public async Task<IActionResult> GetRedemptions(
        int id,
        [FromQuery] string? status,
        [FromQuery] string? channel,
        [FromQuery] string? search,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await promotionRedemptionService.GetAsync(
                new PromotionRedemptionListFilter(
                    id,
                    status,
                    channel,
                    search,
                    fromDate,
                    toDate
                ),
                page,
                pageSize,
                cancellationToken
            )
        );

    [EnableRateLimiting(RateLimitingExtensions.ExportPolicy)]
    [HttpGet("{id:int}/redemptions/export")]
    public async Task<IActionResult> ExportRedemptions(
        int id,
        [FromQuery] string? status,
        [FromQuery] string? channel,
        [FromQuery] string? search,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? format,
        CancellationToken cancellationToken
    )
    {
        var file = await exportJobService.ExportNowAsync(
            CurrentActor,
            ImportExportModules.PromotionRedemptions,
            new PromotionRedemptionListFilter(id, status, channel, search, fromDate, toDate),
            ExportFormats.Parse(format),
            cancellationToken
        );
        return File(file.Content, file.ContentType, file.FileName);
    }

    [EnableRateLimiting(RateLimitingExtensions.ExportPolicy)]
    [HttpPost("{id:int}/redemptions/export-jobs")]
    [SuccessMessage(nameof(Messages.Success_ExportJobQueued))]
    public async Task<IActionResult> CreateRedemptionsExportJob(
        int id,
        [FromQuery] string? status,
        [FromQuery] string? channel,
        [FromQuery] string? search,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] string? format,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status202Accepted,
            await exportJobService.EnqueueAsync(
                CurrentActor,
                ImportExportModules.PromotionRedemptions,
                new PromotionRedemptionListFilter(id, status, channel, search, fromDate, toDate),
                ExportFormats.Parse(format),
                cancellationToken
            )
        );

    [HttpPost("personal-codes/bulk")]
    [SuccessMessage(nameof(Messages.Success_PromotionPersonalCodesCreated))]
    public async Task<IActionResult> CreatePersonalCodes(
        [FromBody] BulkPersonalCodesRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await promotionBulkCodeService.CreateAsync(request, cancellationToken)
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await promotionService.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_PromotionCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertPromotionRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await promotionService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_PromotionUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertPromotionRequest request,
        CancellationToken cancellationToken
    ) => Ok(await promotionService.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/pause")]
    [SuccessMessage(nameof(Messages.Success_PromotionPaused))]
    public async Task<IActionResult> Pause(int id, CancellationToken cancellationToken) =>
        Ok(await promotionService.PauseAsync(id, cancellationToken));

    [HttpPost("{id:int}/resume")]
    [SuccessMessage(nameof(Messages.Success_PromotionResumed))]
    public async Task<IActionResult> Resume(int id, CancellationToken cancellationToken) =>
        Ok(await promotionService.ResumeAsync(id, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_PromotionDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await promotionService.DeleteAsync(id, cancellationToken));
}
