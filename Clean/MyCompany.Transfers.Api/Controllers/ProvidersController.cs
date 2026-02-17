using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Providers.Commands;
using MyCompany.Transfers.Application.Providers.Dtos;
using MyCompany.Transfers.Application.Providers.Queries;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Административный CRUD для справочника провайдеров.
/// </summary>
[ApiController]
[Route("api/admin/providers")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize] // Авторизация через JWT Bearer токен с ролями
public sealed class ProvidersController : BaseController
{
    private readonly ISender _mediator;

    public ProvidersController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProvidersQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProviderByIdQuery(id), ct);
        return result.Match(p => Ok(ProviderAdminDto.FromDomain(p)), Problem);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProviderAdminDto dto, CancellationToken ct)
    {
        var cmd = new CreateProviderCommand(
            dto.Id,
            dto.Name,
            dto.BaseUrl,
            dto.TimeoutSeconds,
            dto.AuthType,
            dto.SettingsJson,
            dto.IsEnabled,
            dto.FeePermille);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(created => CreatedAtAction(nameof(GetById), new { id = created.Id }, created), Problem);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ProviderAdminDto dto, CancellationToken ct)
    {
        var cmd = new UpdateProviderCommand(
            id,
            dto.Name,
            dto.BaseUrl,
            dto.TimeoutSeconds,
            dto.AuthType,
            dto.SettingsJson,
            dto.IsEnabled,
            dto.FeePermille);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(updated => Ok(updated), Problem);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var cmd = new DeleteProviderCommand(id);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(_ => NoContent(), Problem);
    }
}
