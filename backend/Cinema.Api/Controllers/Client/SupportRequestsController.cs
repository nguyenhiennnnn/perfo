using Cinema.Api.Filters;
using Cinema.Application.DTOs.Support;
using Cinema.Application.Interfaces.Support;
using Cinema.Domain.Enums;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Client;

[ApiController]
[Route(ApiPaths.ApiVersionPrefix + "/support-requests")]
[Authorize]
public class SupportRequestsController(ISupportRequestService supportRequestService)
    : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] SupportRequestStatus? status,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await supportRequestService.GetForCustomerAsync(
                CurrentUserId,
                status,
                page,
                pageSize,
                cancellationToken
            )
        );

    [HttpGet("{requestId:int}")]
    public async Task<ActionResult<SupportRequestDto>> Get(
        int requestId,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await supportRequestService.GetForCustomerAsync(
                CurrentUserId,
                requestId,
                cancellationToken
            )
        );

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_SupportRequestCreated))]
    public async Task<ActionResult<SupportRequestDto>> Create(
        [FromBody] CreateSupportRequestRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await supportRequestService.CreateForCustomerAsync(
                CurrentUserId,
                request,
                cancellationToken
            )
        );

    [HttpPost("{requestId:int}/messages")]
    [SuccessMessage(nameof(Messages.Success_SupportMessageSent))]
    public async Task<ActionResult<SupportRequestDto>> Reply(
        int requestId,
        [FromBody] ReplySupportRequestRequest request,
        CancellationToken cancellationToken
    ) =>
        Ok(
            await supportRequestService.ReplyForCustomerAsync(
                CurrentUserId,
                requestId,
                request,
                cancellationToken
            )
        );
}
