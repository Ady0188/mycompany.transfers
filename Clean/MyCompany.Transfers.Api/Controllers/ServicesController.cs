using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Services.Commands;
using MyCompany.Transfers.Application.Services.Dtos;
using MyCompany.Transfers.Application.Services.Queries;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Административный CRUD для справочника услуг.
/// </summary>
[ApiController]
[Route("api/admin/services")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class ServicesController : BaseController
{
    private readonly ISender _mediator;

    public ServicesController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetServicesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetServiceByIdAdminQuery(id), ct);
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ServiceAdminDto dto, CancellationToken ct)
    {
        var cmd = new CreateServiceCommand(
            dto.Id,
            dto.ProviderId,
            dto.ProviderServiceId,
            dto.Name,
            dto.AllowedCurrencies,
            dto.FxRounding,
            dto.MinAmountMinor,
            dto.MaxAmountMinor,
            dto.AccountDefinitionId,
            dto.Parameters);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(created => CreatedAtAction(nameof(GetById), new { id = created.Id }, created), Problem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ServiceAdminDto dto, CancellationToken ct)
    {
        var cmd = new UpdateServiceCommand(
            id,
            dto.ProviderId,
            dto.ProviderServiceId,
            dto.Name,
            dto.AllowedCurrencies,
            dto.FxRounding,
            dto.MinAmountMinor,
            dto.MaxAmountMinor,
            dto.AccountDefinitionId,
            dto.Parameters);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(updated => Ok(updated), Problem);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteServiceCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
