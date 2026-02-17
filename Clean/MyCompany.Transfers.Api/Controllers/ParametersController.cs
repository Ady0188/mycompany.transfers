using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Parameters.Commands;
using MyCompany.Transfers.Application.Parameters.Dtos;
using MyCompany.Transfers.Application.Parameters.Queries;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Административный CRUD для справочника параметров (ParamDefinition).
/// </summary>
[ApiController]
[Route("api/admin/parameters")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class ParametersController : BaseController
{
    private readonly ISender _mediator;

    public ParametersController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetParametersQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetParameterByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ParameterAdminDto dto, CancellationToken ct)
    {
        var cmd = new CreateParameterCommand(dto.Id, dto.Code, dto.Name, dto.Description, dto.Regex, dto.Active);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(created => CreatedAtAction(nameof(GetById), new { id = created.Id }, created), Problem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ParameterAdminDto dto, CancellationToken ct)
    {
        var cmd = new UpdateParameterCommand(id, dto.Code, dto.Name, dto.Description, dto.Regex, dto.Active);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(updated => Ok(updated), Problem);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteParameterCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
