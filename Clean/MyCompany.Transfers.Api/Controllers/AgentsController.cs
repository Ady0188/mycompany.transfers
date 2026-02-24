using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Agents.Commands;
using MyCompany.Transfers.Application.Agents.Dtos;
using MyCompany.Transfers.Application.Agents.Queries;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Административный CRUD для справочника агентов.
/// </summary>
[ApiController]
[Route("api/admin/agents")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize] // Авторизация через JWT Bearer токен с ролями
public sealed class AgentsController : BaseController
{
    private readonly ISender _mediator;

    public AgentsController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAgentsQuery(page, pageSize, search), ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAgentByIdQuery(id), ct);
        return result.Match(a => Ok(AgentAdminDto.FromDomain(a)), Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AgentAdminDto dto, CancellationToken ct)
    {
        var cmd = new CreateAgentCommand(dto.Id, dto.Account, dto.Name, dto.TimeZoneId, dto.SettingsJson, dto.PartnerEmail, dto.Locale ?? "ru");
        var result = await _mediator.Send(cmd, ct);
        return result.Match(created => CreatedAtAction(nameof(GetById), new { id = created.Id }, created), Problem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] AgentAdminDto dto, CancellationToken ct)
    {
        var cmd = new UpdateAgentCommand(id, dto.Account, dto.Name, dto.TimeZoneId, dto.SettingsJson, dto.PartnerEmail, dto.Locale);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(updated => Ok(updated), Problem);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var cmd = new DeleteAgentCommand(id);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(_ => NoContent(), Problem);
    }

    /// <summary>История отправленных писем с данными терминалов агенту.</summary>
    [HttpGet("{id}/sent-credentials-history")]
    public async Task<IActionResult> GetSentCredentialsHistory(string id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetSentCredentialsHistoryQuery(id), ct);
        return result.Match(list => Ok(list), Problem);
    }
}

