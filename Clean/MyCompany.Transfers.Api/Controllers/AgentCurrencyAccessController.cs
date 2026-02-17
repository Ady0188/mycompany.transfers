using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Access.CurrencyAccess.Commands;
using MyCompany.Transfers.Application.Access.CurrencyAccess.Dtos;
using MyCompany.Transfers.Application.Access.CurrencyAccess.Queries;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Административный CRUD для доступа агент–валюта (AgentCurrencyAccess).
/// </summary>
[ApiController]
[Route("api/admin/access/currencies")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class AgentCurrencyAccessController : BaseController
{
    private readonly ISender _mediator;

    public AgentCurrencyAccessController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAgentCurrencyAccessListQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{agentId}/{currency}")]
    public async Task<IActionResult> GetByKey(string agentId, string currency, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAgentCurrencyAccessByKeyQuery(agentId, currency), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AgentCurrencyAccessAdminDto dto, CancellationToken ct)
    {
        var cmd = new CreateAgentCurrencyAccessCommand(dto.AgentId, dto.Currency, dto.Enabled);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(
            created => CreatedAtAction(nameof(GetByKey), new { agentId = created.AgentId, currency = created.Currency }, created),
            Problem);
    }

    [HttpPut("{agentId}/{currency}")]
    public async Task<IActionResult> Update(string agentId, string currency, [FromBody] AgentCurrencyAccessAdminDto dto, CancellationToken ct)
    {
        var cmd = new UpdateAgentCurrencyAccessCommand(agentId, currency, dto.Enabled);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(updated => Ok(updated), Problem);
    }

    [HttpDelete("{agentId}/{currency}")]
    public async Task<IActionResult> Delete(string agentId, string currency, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteAgentCurrencyAccessCommand(agentId, currency), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
