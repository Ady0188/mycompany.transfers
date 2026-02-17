using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Agents.Commands;
using MyCompany.Transfers.Api.Auth;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Эндпоинты для АБС.
/// Авторизация: заголовок X-Abs-Api-Key (конфиг Abs:ApiKey).
/// Не используются админ-панелью и не терминалами агентов.
/// </summary>
[ApiController]
[Route("api/abs")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "abs")]
[AbsApiKeyAuthorize]
public sealed class AbsController : BaseController
{
    private readonly ISender _mediator;

    public AbsController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Кредитование (зачисление) на баланс агента.
    /// </summary>
    [HttpPost("agents/{agentId}/credit")]
    public async Task<IActionResult> Credit(string agentId, [FromBody] AbsBalanceRequest body, CancellationToken ct)
    {
        var cmd = new CreditAgentCommand(agentId, body.Currency, body.AmountMinor);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(ok => Ok(ok), Problem);
    }

    /// <summary>
    /// Дебитование (списание) с баланса агента.
    /// </summary>
    [HttpPost("agents/{agentId}/debit")]
    public async Task<IActionResult> Debit(string agentId, [FromBody] AbsBalanceRequest body, CancellationToken ct)
    {
        var cmd = new DebitAgentCommand(agentId, body.Currency, body.AmountMinor);
        var result = await _mediator.Send(cmd, ct);
        return result.Match(ok => Ok(ok), Problem);
    }
}

/// <summary>
/// Тело запроса для кредитования/дебитования.
/// </summary>
public sealed record AbsBalanceRequest(string Currency, long AmountMinor);
