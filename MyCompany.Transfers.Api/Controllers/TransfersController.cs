using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Transfers.Dtos;
using MyCompany.Transfers.Application.Transfers.Queries;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Административный просмотр списка переводов.
/// </summary>
[ApiController]
[Route("api/admin/transfers")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class TransfersController : BaseController
{
    private readonly ISender _mediator;

    public TransfersController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? id = null,
        [FromQuery] string? agentId = null,
        [FromQuery] string? externalId = null,
        [FromQuery] string? providerId = null,
        [FromQuery] string? serviceId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTimeOffset? createdFrom = null,
        [FromQuery] DateTimeOffset? createdTo = null,
        [FromQuery] string? account = null,
        CancellationToken ct = default)
    {
        var filter = new TransfersFilter(id, agentId, externalId, providerId, serviceId, status, createdFrom, createdTo, account);
        var result = await _mediator.Send(new GetTransfersQuery(page, pageSize, filter), ct);
        return Ok(result);
    }
}
