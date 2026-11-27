using Cinema.Api.Filters;
using Cinema.Application.DTOs.Memberships;
using Cinema.Application.Interfaces.Memberships;
using Cinema.Application.Interfaces.Orders;
using Cinema.Application.Utils;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/membership-subscriptions")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminMembershipSubscriptionsController(
    IMembershipRefundService refundService,
    IMembershipSubscriptionQueryService queryService
) : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? planId,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await queryService.GetAdminAsync(
                page,
                pageSize,
                new MembershipSubscriptionListFilter(search, status, planId),
                cancellationToken
            )
        );

    [HttpPost("{id:int}/refund")]
    [SuccessMessage(nameof(Messages.Success_MembershipSubscriptionRefunded))]
    public async Task<ActionResult<MembershipRefundResultDto>> Refund(
        int id,
        [FromBody] RefundMembershipSubscriptionRequest request,
        CancellationToken cancellationToken
    )
    {
        EnumConverters.TryParseEnum<Cinema.Domain.Enums.MembershipRefundReason>(
            request.Reason,
            out var reason
        );
        var result = await refundService.RefundAsync(
            id,
            new CancelActor(CancelActorKind.Admin, CurrentUserId),
            reason,
            request.Note,
            cancellationToken
        );
        return Ok(result);
    }
}
