using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Terminals.Commands;
using MyCompany.Transfers.Application.Terminals.Dtos;
using MyCompany.Transfers.Application.Terminals.Queries;
using MyCompany.Transfers.Api.Auth;
using MyCompany.Transfers.Api.Models;

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
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTerminalsQuery(page, pageSize, search), ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTerminalByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TerminalAdminDto dto, CancellationToken ct = default)
    {
        var cmd = new CreateTerminalCommand(dto.AgentId, dto.Name, dto.ApiKey, dto.Active);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(created => CreatedAtAction(nameof(GetById), new { id = created.Id }, created), Problem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] TerminalAdminDto dto, CancellationToken ct = default)
    {
        var cmd = new UpdateTerminalCommand(id, dto.AgentId, dto.Name, dto.ApiKey, null, dto.Active);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(updated => Ok(updated), Problem);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteTerminalCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    /// <summary>Отправить на почту зашифрованный архив с данными терминала (AES). Пароль возвращается один раз — передайте партнёру другим каналом (Telegram, WhatsApp и т.д.).</summary>
    [HttpPost("{id}/send-credentials")]
    public async Task<IActionResult> SendCredentials(string id, [FromBody] SendTerminalCredentialsRequest request, CancellationToken ct = default)
    {
        var cmd = new SendTerminalCredentialsCommand(id, request.ToEmail, request.Body, request.Subject);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(
            r => Ok(new SendCredentialsResponse { ArchivePassword = r.ArchivePassword }),
            Problem);
    }
}
