using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Bins.Commands;
using MyCompany.Transfers.Application.Bins.Dtos;
using MyCompany.Transfers.Application.Bins.Queries;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Административный CRUD для справочника БИН.
/// С клиента при создании/обновлении передаются только Prefix, Code, Name. Len вычисляется на сервере.
/// </summary>
[ApiController]
[Route("api/admin/bins")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class BinsController : BaseController
{
    private readonly ISender _mediator;

    public BinsController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBinsQuery(page, pageSize, search), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBinByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BinAdminDto dto, CancellationToken ct = default)
    {
        var cmd = new CreateBinCommand(dto.Prefix ?? "", dto.Code ?? "", dto.Name ?? "");
        var result = await _mediator.Send(cmd, ct);
        return result.Match(created => CreatedAtAction(nameof(GetById), new { id = created.Id }, created), Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BinAdminDto dto, CancellationToken ct = default)
    {
        var cmd = new UpdateBinCommand(id, dto.Prefix ?? "", dto.Code ?? "", dto.Name ?? "");
        var result = await _mediator.Send(cmd, ct);
        return result.Match(updated => Ok(updated), Problem);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new DeleteBinCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
