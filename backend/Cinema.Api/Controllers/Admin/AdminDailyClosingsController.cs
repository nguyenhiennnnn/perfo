using Cinema.Api.Filters;
using Cinema.Application.DTOs.DailyClosings;
using Cinema.Application.Interfaces.DailyClosings;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/daily-closings")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminDailyClosingsController(
    IDailyClosingReportService reportService,
    IDailyClosingLedgerReportService ledgerReportService,
    IDailyClosingService closingService
) : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DailyClosingListFilter filter,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) => Ok(await closingService.GetClosingsAsync(filter, page, pageSize, cancellationToken));

    [HttpGet("draft")]
    public async Task<IActionResult> GetDraft(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken
    ) => Ok(await reportService.BuildDraftAsync(date, cancellationToken));

    [HttpGet("{date}")]
    public async Task<IActionResult> GetByDate(DateOnly date, CancellationToken cancellationToken) =>
        Ok(await closingService.GetByDateAsync(date, cancellationToken));

    [HttpGet("{date}/report")]
    public async Task<IActionResult> GetReport(
        DateOnly date,
        [FromQuery] DailyClosingReportMode mode = DailyClosingReportMode.OriginalDate,
        CancellationToken cancellationToken = default
    ) => Ok(await ledgerReportService.GetAsync(date, mode, cancellationToken));

    [HttpPost("{date}/close")]
    [SuccessMessage(nameof(Messages.Success_DailyClosingClosed))]
    public async Task<IActionResult> Close(
        DateOnly date,
        [FromBody] CloseDailyClosingRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await closingService.CloseAsync(CurrentUserId, date, request, cancellationToken)
        );
}
