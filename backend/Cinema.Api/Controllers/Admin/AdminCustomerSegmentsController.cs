using Cinema.Api.Filters;
using Cinema.Application.DTOs.Segments;
using Cinema.Application.Interfaces.Segments;
using Cinema.Shared.Constants;
using Cinema.Shared.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.Api.Controllers.Admin;

[ApiController]
[Route(ApiPaths.AdminRoutePrefix + "/customer-segments")]
[Authorize(Policy = Policies.AdminOnly)]
public class AdminCustomerSegmentsController(
    ICustomerSegmentAdminService segmentService,
    ICustomerSegmentQueryService queryService
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await segmentService.GetAsync(
                page,
                pageSize,
                new CustomerSegmentListFilter(search, isActive),
                cancellationToken
            )
        );

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await segmentService.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:int}/customers")]
    public async Task<IActionResult> GetCustomers(
        int id,
        [FromQuery] string? search,
        [FromQuery] bool eligibleOnly = false,
        [FromQuery] bool revealPersonalData = false,
        [FromQuery] int page = Pagination.DefaultPage,
        [FromQuery] int pageSize = Pagination.DefaultPageSize,
        CancellationToken cancellationToken = default
    ) =>
        Ok(
            await queryService.GetCustomersAsync(
                id,
                page,
                pageSize,
                new SegmentCustomerListFilter(search, eligibleOnly, revealPersonalData),
                cancellationToken
            )
        );

    [HttpPost]
    [SuccessMessage(nameof(Messages.Success_CustomerSegmentCreated))]
    public async Task<IActionResult> Create(
        [FromBody] UpsertCustomerSegmentRequest request,
        CancellationToken cancellationToken
    ) =>
        StatusCode(
            StatusCodes.Status201Created,
            await segmentService.CreateAsync(request, cancellationToken)
        );

    [HttpPut("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_CustomerSegmentUpdated))]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpsertCustomerSegmentRequest request,
        CancellationToken cancellationToken
    ) => Ok(await segmentService.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [SuccessMessage(nameof(Messages.Success_CustomerSegmentDeleted))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Ok(await segmentService.DeleteAsync(id, cancellationToken));
}
