using Cinema.Api.Extensions;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.GiftCards;
using Cinema.Application.DTOs.Import;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/gift-cards")]
[Authorize]
public class GiftCardsController(
    IGiftCardPurchaseService purchaseService,
    IGiftCardLookupService lookupService,
    IGiftCardPurchasedQueryService purchasedQueryService,
    IImportService<GiftCardBulkImportRow> bulkImportService,
    IGiftCardBulkOrderService bulkOrderService
) : AppControllerBase
{
    [HttpPost("purchase")]
    [EnableRateLimiting(RateLimitingExtensions.BookingHoldPolicy)]
    public async Task<ActionResult<HoldOrderResponse>> Purchase(
        [FromBody] PurchaseGiftCardRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await purchaseService.PurchaseAsync(CurrentUserId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("purchased")]
    public async Task<ActionResult<CursorPageResult<PurchasedGiftCardDto>>> GetPurchased(
        [FromQuery] string? cursor,
        [FromQuery] int? limit = null,
        CancellationToken cancellationToken = default
    )
    {
        var result = await purchasedQueryService.GetPurchasedAsync(
            CurrentUserId,
            cursor,
            ClampLimit(limit),
            cancellationToken
        );
        return Ok(result);
    }

    [HttpPost("balance")]
    [EnableRateLimiting(RateLimitingExtensions.BookingHoldPolicy)]
    public async Task<ActionResult<GiftCardLookupDto>> CheckBalance(
        [FromBody] ApplyGiftCardRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await lookupService.CheckBalanceAsync(
            CurrentUserId,
            request.Code,
            cancellationToken
        );
        return Ok(result);
    }

    [EnableRateLimiting(RateLimitingExtensions.ImportPolicy)]
    [HttpGet("bulk/import/template")]
    public async Task<IActionResult> DownloadBulkTemplate(CancellationToken cancellationToken)
    {
        var file = await bulkImportService.GetTemplateAsync(cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [EnableRateLimiting(RateLimitingExtensions.ImportPolicy)]
    [HttpPost("bulk/import/preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImportLimits.MaxRequestBytes)]
    public async Task<IActionResult> PreviewBulkImport(
        IFormFile? file = null,
        CancellationToken cancellationToken = default
    )
    {
        await using var input = file.ToImportInput();
        var result = await bulkImportService.PreviewAsync(CurrentActor, input, cancellationToken);
        return Ok(result);
    }

    [EnableRateLimiting(RateLimitingExtensions.BookingHoldPolicy)]
    [HttpPost("bulk/import/apply")]
    public async Task<ActionResult<HoldOrderResponse>> ApplyBulkImport(
        [FromBody] ImportApplyRequest request,
        CancellationToken cancellationToken
    )
    {
        var result = await bulkOrderService.CreateOrderAsync(
            CurrentUserId,
            request,
            cancellationToken
        );
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [EnableRateLimiting(RateLimitingExtensions.ImportPolicy)]
    [HttpGet("bulk/import/errors/{previewId}")]
    public async Task<IActionResult> DownloadBulkImportErrors(
        string previewId,
        CancellationToken cancellationToken
    )
    {
        var file = await bulkImportService.GetErrorFileAsync(
            CurrentUserId,
            previewId,
            cancellationToken
        );
        return File(file.Content, file.ContentType, file.FileName);
    }
}
