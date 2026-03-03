using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.AccountDefinitions.Commands;
using MyCompany.Transfers.Application.AccountDefinitions.Dtos;
using MyCompany.Transfers.Application.AccountDefinitions.Queries;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Административный CRUD для справочника определений счёта (AccountDefinition).
/// </summary>
[ApiController]
[Route("api/admin/account-definitions")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class AccountDefinitionsController : BaseController
{
    private readonly ISender _mediator;

    public AccountDefinitionsController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAccountDefinitionsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAccountDefinitionByIdQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AccountDefinitionAdminDto dto, CancellationToken ct)
    {
        var id = dto.Id == default ? (Guid?)null : dto.Id;
        var cmd = new CreateAccountDefinitionCommand(id, dto.Code, dto.Regex, dto.Normalize, dto.Algorithm, dto.MinLength, dto.MaxLength);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(created => CreatedAtAction(nameof(GetById), new { id = created.Id }, created), Problem);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AccountDefinitionAdminDto dto, CancellationToken ct)
    {
        var cmd = new UpdateAccountDefinitionCommand(id, dto.Code, dto.Regex, dto.Normalize, dto.Algorithm, dto.MinLength, dto.MaxLength);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(updated => Ok(updated), Problem);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteAccountDefinitionCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
