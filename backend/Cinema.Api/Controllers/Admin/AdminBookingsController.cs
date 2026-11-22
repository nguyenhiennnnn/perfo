using Cinema.Api.Extensions;
using Cinema.Api.Filters;
using Cinema.Application.DTOs.Common;
using Cinema.Application.DTOs.Orders;
using Cinema.Application.Interfaces;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/bookings")]
[Authorize]
public class AdminBookingsController(
    IOrderQueryService orderQueryService,
    IOrderTicketPdfService orderTicketPdfService,
    IExportJobService exportJobService
) : AppControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<ActionResult<OffsetPageResult<OrderListItemDto>>> GetOrders(
        [FromQuery] OrderListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    )
    {
        var result = await orderQueryService.GetOrdersAdminAsync(
            filter,
            page,
            pageSize,
            cancellationToken
        );
        return Ok(result);
    }

    [EnableRateLimiting(RateLimitingExtensions.ExportPolicy)]
    [HttpGet("export")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<IActionResult> ExportOrders(
        [FromQuery] OrderListFilter filter,
        [FromQuery] string? format,
        CancellationToken cancellationToken
    )
    {
        var exportFormat = ExportFormats.Parse(format);
        var file = await exportJobService.ExportNowAsync(
            CurrentActor,
            ImportExportModules.Orders,
            filter,
            exportFormat,
            cancellationToken
        );
        return File(file.Content, file.ContentType, file.FileName);
    }

    [EnableRateLimiting(RateLimitingExtensions.ExportPolicy)]
    [HttpPost("export-jobs")]
    [SuccessMessage(nameof(Messages.Success_ExportJobQueued))]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<IActionResult> CreateExportJob(
        [FromQuery] OrderListFilter filter,
        [FromQuery] string? format,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status202Accepted,
            await exportJobService.EnqueueAsync(
                CurrentActor,
                ImportExportModules.Orders,
                filter,
                ExportFormats.Parse(format),
                cancellationToken
            )
        );

    [HttpGet("{orderId:int}")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<ActionResult<OrderDetailDto>> GetOrderDetail(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var result = await orderQueryService.GetOrderDetailAdminAsync(orderId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{orderId:int}/tickets/pdf")]
    [Authorize(Policy = Policies.StaffOnly)]
    public async Task<IActionResult> DownloadTicketsPdf(
        int orderId,
        CancellationToken cancellationToken
    )
    {
        var file = await orderTicketPdfService.GenerateForStaffAsync(orderId, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
