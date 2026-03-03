using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Access.ServiceAccess.Commands;
using MyCompany.Transfers.Application.Access.ServiceAccess.Dtos;
using MyCompany.Transfers.Application.Access.ServiceAccess.Queries;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Административный CRUD для доступа агент–услуга (AgentServiceAccess).
/// </summary>
[ApiController]
[Route("api/admin/access/services")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class AgentServiceAccessController : BaseController
{
    private readonly ISender _mediator;

    public AgentServiceAccessController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAgentServiceAccessListQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{agentId}/{serviceId}")]
    public async Task<IActionResult> GetByKey(string agentId, string serviceId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAgentServiceAccessByKeyQuery(agentId, serviceId), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AgentServiceAccessAdminDto dto, CancellationToken ct)
    {
        var cmd = new CreateAgentServiceAccessCommand(dto.AgentId, dto.ServiceId, dto.Enabled, dto.FeePermille, dto.FeeFlatMinor);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(
            created => CreatedAtAction(nameof(GetByKey), new { agentId = created.AgentId, serviceId = created.ServiceId }, created),
            Problem);
    }

    [HttpPut("{agentId}/{serviceId}")]
    public async Task<IActionResult> Update(string agentId, string serviceId, [FromBody] AgentServiceAccessAdminDto dto, CancellationToken ct)
    {
        var cmd = new UpdateAgentServiceAccessCommand(agentId, serviceId, dto.Enabled, dto.FeePermille, dto.FeeFlatMinor);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(updated => Ok(updated), Problem);
    }

    [HttpDelete("{agentId}/{serviceId}")]
    public async Task<IActionResult> Delete(string agentId, string serviceId, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteAgentServiceAccessCommand(agentId, serviceId), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
