using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Terminals.Commands;
using MyCompany.Transfers.Application.Terminals.Dtos;
using MyCompany.Transfers.Application.Terminals.Queries;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Административный CRUD для справочника терминалов.
/// </summary>
[ApiController]
[Route("api/admin/terminals")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class TerminalsController : BaseController
{
    private readonly ISender _mediator;

    public TerminalsController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTerminalsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTerminalByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TerminalAdminDto dto, CancellationToken ct)
    {
        var cmd = new CreateTerminalCommand(dto.Id, dto.AgentId, dto.Name, dto.ApiKey, dto.Secret, dto.Active);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(created => CreatedAtAction(nameof(GetById), new { id = created.Id }, created), Problem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] TerminalAdminDto dto, CancellationToken ct)
    {
        var cmd = new UpdateTerminalCommand(id, dto.AgentId, dto.Name, dto.ApiKey, dto.Secret, dto.Active);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(updated => Ok(updated), Problem);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteTerminalCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
