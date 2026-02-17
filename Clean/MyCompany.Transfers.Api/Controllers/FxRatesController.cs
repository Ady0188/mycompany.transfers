using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Rates.Commands;
using MyCompany.Transfers.Application.Rates.Dtos;
using MyCompany.Transfers.Application.Rates.Queries;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Курсы валют: добавление/обновление (авторизация АБС — X-Abs-Api-Key), чтение списка/по ключу (админ — JWT Bearer).
/// </summary>
[ApiController]
[Route("api/admin/fx-rates")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
public sealed class FxRatesController : BaseController
{
    private readonly ISender _mediator;

    public FxRatesController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Добавить новый курс или обновить существующий (по AgentId + BaseCurrency + QuoteCurrency). Авторизация: X-Abs-Api-Key (АБС).
    /// </summary>
    [HttpPost]
    [AbsApiKeyAuthorize]
    public async Task<IActionResult> Upsert([FromBody] FxRateAdminDto dto, CancellationToken ct)
    {
        var cmd = new UpsertFxRateCommand(dto.AgentId, dto.BaseCurrency, dto.QuoteCurrency, dto.Rate, dto.Source);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(created => Ok(created), Problem);
    }

    /// <summary>
    /// Список курсов для админ-панели. Опционально фильтр по agentId (query). Авторизация: JWT Bearer (админ).
    /// </summary>
    [HttpGet]
    [AdminRoleAuthorize]
    public async Task<IActionResult> GetAll([FromQuery] string? agentId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFxRatesForAdminQuery(agentId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Получить курс по ключу (AgentId, BaseCurrency, QuoteCurrency). Авторизация: JWT Bearer (админ).
    /// </summary>
    [HttpGet("{agentId}/{baseCurrency}/{quoteCurrency}")]
    [AdminRoleAuthorize]
    public async Task<IActionResult> GetByKey(string agentId, string baseCurrency, string quoteCurrency, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFxRateByKeyForAdminQuery(agentId, baseCurrency, quoteCurrency), ct);
        return result.Match(dto => Ok(dto), Problem);
    }
}
