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
[Route(ApiPaths.AdminRoutePrefix + "/refund-proposals")]
[Authorize(Policy = Policies.StaffOnly)]
public class AdminRefundProposalsController(IExceptionalRefundService exceptionalRefundService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] ExceptionalRefundStatus? status,
        [FromQuery] int? orderId,
        [FromQuery] int? proposedByUserId,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await exceptionalRefundService.GetAsync(
                new ExceptionalRefundProposalListFilter(status, orderId, proposedByUserId),
                page,
                pageSize,
                cancellationToken
            )
        );

    [HttpGet("{proposalId:int}")]
    public async Task<ActionResult<ExceptionalRefundProposalDto>> Get(
        int proposalId,
        CancellationToken cancellationToken
    ) => Ok(await exceptionalRefundService.GetByIdAsync(proposalId, cancellationToken));

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_ExceptionalRefundProposed))]
    public async Task<ActionResult<ExceptionalRefundProposalDto>> Propose(
        [FromBody] CreateExceptionalRefundProposalRequest request,
        CancellationToken cancellationToken
    ) => Ok(await exceptionalRefundService.ProposeAsync(request, cancellationToken));

    [HttpPost("{proposalId:int}/approve")]
    [Authorize(Policy = Policies.AdminOnly)]
    [SuccessMessage(nameof(Messages.Success_ExceptionalRefundApproved))]
    public async Task<ActionResult<ExceptionalRefundProposalDto>> Approve(
        int proposalId,
        [FromBody] ApproveExceptionalRefundProposalRequest request,
        CancellationToken cancellationToken
    ) => Ok(await exceptionalRefundService.ApproveAsync(proposalId, request, cancellationToken));

    [HttpPost("{proposalId:int}/reject")]
    [Authorize(Policy = Policies.AdminOnly)]
    [SuccessMessage(nameof(Messages.Success_ExceptionalRefundRejected))]
    public async Task<ActionResult<ExceptionalRefundProposalDto>> Reject(
        int proposalId,
        [FromBody] RejectExceptionalRefundProposalRequest request,
        CancellationToken cancellationToken
    ) => Ok(await exceptionalRefundService.RejectAsync(proposalId, request, cancellationToken));
}
