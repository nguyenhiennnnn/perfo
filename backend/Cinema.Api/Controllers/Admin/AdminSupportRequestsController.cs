using Cinema.Api.Filters;
using Cinema.Application.DTOs.Support;
using Cinema.Application.Interfaces.Support;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/support-requests")]
[Authorize(Policy = Policies.StaffOnly)]
public class AdminSupportRequestsController(ISupportRequestStaffService supportRequestStaffService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] SupportRequestStatus? status,
        [FromQuery] int? assigneeId,
        [FromQuery] bool? unassigned,
        [FromQuery] string? search,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await supportRequestStaffService.GetAsync(
                new SupportRequestStaffListFilter(status, assigneeId, unassigned, search),
                page,
                pageSize,
                cancellationToken
            )
        );

    [HttpGet("{requestId:int}")]
    public async Task<ActionResult<StaffSupportRequestDto>> Get(
        int requestId,
        CancellationToken cancellationToken
    ) => Ok(await supportRequestStaffService.GetByIdAsync(requestId, cancellationToken));

    [HttpPut("{requestId:int}/assignee")]
    [SuccessMessage(nameof(Messages.Success_SupportAssigned))]
    public async Task<ActionResult<StaffSupportRequestDto>> Assign(
        int requestId,
        [FromBody] AssignSupportRequestRequest request,
        CancellationToken cancellationToken
    ) => Ok(await supportRequestStaffService.AssignAsync(requestId, request, cancellationToken));

    [HttpPut("{requestId:int}/status")]
    [SuccessMessage(nameof(Messages.Success_SupportStatusChanged))]
    public async Task<ActionResult<StaffSupportRequestDto>> ChangeStatus(
        int requestId,
        [FromBody] ChangeSupportRequestStatusRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await supportRequestStaffService.ChangeStatusAsync(
                requestId,
                request,
                cancellationToken
            )
        );

    [HttpPost("{requestId:int}/messages")]
    [SuccessMessage(nameof(Messages.Success_SupportStaffReplied))]
    public async Task<ActionResult<StaffSupportRequestDto>> Reply(
        int requestId,
        [FromBody] StaffReplySupportRequestRequest request,
        CancellationToken cancellationToken
    ) => Ok(await supportRequestStaffService.ReplyAsync(requestId, request, cancellationToken));

    [HttpPost("{requestId:int}/apology-code")]
    [SuccessMessage(nameof(Messages.Success_SupportApologyCodeIssued))]
    public async Task<ActionResult<StaffSupportRequestDto>> IssueApologyCode(
        int requestId,
        [FromBody] IssueSupportApologyCodeRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await supportRequestStaffService.IssueApologyCodeAsync(
                requestId,
                request,
                cancellationToken
            )
        );

    [HttpPost("{requestId:int}/resend-ticket-email")]
    [SuccessMessage(nameof(Messages.Success_SupportTicketEmailResent))]
    public async Task<ActionResult<StaffSupportRequestDto>> ResendTicketEmail(
        int requestId,
        CancellationToken cancellationToken
    ) =>
        Ok(await supportRequestStaffService.ResendTicketEmailAsync(requestId, cancellationToken));
}
