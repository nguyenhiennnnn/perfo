using Cinema.Api.Filters;
using Cinema.Application.DTOs.Campaigns;
using Cinema.Application.Interfaces.Campaigns;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/campaigns")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminCampaignsController(
    ICampaignAdminService campaignService,
    ICampaignAudienceService audienceService,
    ICampaignTestSendService testSendService
) : AppControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? segmentId,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await campaignService.GetAsync(
                page,
                pageSize,
                new CampaignListFilter(search, status, segmentId),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await campaignService.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:int}/recipients")]
    public async Task<IActionResult> GetRecipients(
        int id,
        [FromQuery] string? search,
        [FromQuery] bool revealPersonalData = false,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await audienceService.GetRecipientsAsync(
                id,
                page,
                pageSize,
                new CampaignRecipientListFilter(search, revealPersonalData),
                cancellationToken
            )
        );

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_CampaignCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertCampaignRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await campaignService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_CampaignUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertCampaignRequest request,
        CancellationToken cancellationToken
    ) => Ok(await campaignService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_CampaignDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await campaignService.DeleteAsync(id, cancellationToken));

    [HttpPost("{id:int}/test-send")]
    [SuccessMessage(nameof(Messages.Success_CampaignTestSent))]
    public async Task<IActionResult> TestSend(int id, CancellationToken cancellationToken) =>
        Ok(await testSendService.SendAsync(id, CurrentUserId, cancellationToken));
}
