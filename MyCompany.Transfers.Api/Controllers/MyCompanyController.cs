using MyCompany.Transfers.Api.Auth;
using MyCompany.Transfers.Application.Agents.Queries;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.MyCompanyTransfers.Commands;
using MyCompany.Transfers.Application.MyCompanyTransfers.Queries;
using MyCompany.Transfers.Contract;
using MyCompany.Transfers.Contract.Core.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MyCompany.Transfers.Api.Controllers;

[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "transfers")]
[SignatureAuthorize]
public class MyCompanyController : BaseController
{
    private readonly ISender _mediator;

    public MyCompanyController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet(ApiEndpoints.V1.MyCompanyTransfers.CheckAccount)]
    public async Task<IActionResult> CheckAccount([FromQuery] string serviceId, [FromQuery] string account, [FromQuery] int method, CancellationToken cancellationToken)
    {
        var agentId = User.FindFirstValue("agent_id")!;
        var query = new CheckCommand(agentId, serviceId, (Domain.Transfers.TransferMethod)method, account);
        var result = await _mediator.Send(query, cancellationToken);

        return result.Match(
            res => Ok(res),
            Problem);
    }

    [HttpPost(ApiEndpoints.V1.MyCompanyTransfers.Prepare)]
    public async Task<IActionResult> Prepare([FromBody] PrepareRequest req, CancellationToken ct)
    {
        var agentId = User.FindFirstValue("agent_id")!;
        var terminalId = User.FindFirstValue("terminal_id")!;
        var cmd = new PrepareCommand(
            agentId, terminalId, req.ExternalId,
            (Domain.Transfers.TransferMethod)req.Method,
            req.Account, req.Amount, req.Currency, req.PayoutCurrency, req.ServiceId, req.Parameters);
        var res = await _mediator.Send(cmd, ct);
        return res.Match(dto => Ok(dto), Problem);
    }

    [HttpPost(ApiEndpoints.V1.MyCompanyTransfers.Confirm)]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequest req, CancellationToken ct)
    {
        var agentId = User.FindFirstValue("agent_id")!;
        var terminalId = User.FindFirstValue("terminal_id")!;
        var cmd = new ConfirmCommand(agentId, terminalId, req.ExternalId, req.QuotationId);
        var res = await _mediator.Send(cmd, ct);
        return res.Match(dto => Ok(dto), Problem);
    }

    [HttpGet(ApiEndpoints.V1.MyCompanyTransfers.GetStatus)]
    public async Task<IActionResult> GetStatus(
        [FromQuery] string? externalId,
        [FromQuery] string? transferId,
        CancellationToken ct)
    {
        var agentId = User.FindFirstValue("agent_id")!;
        var q = new GetStatusQuery(agentId, externalId, transferId);
        var res = await _mediator.Send(q, ct);
        return res.Match(dto => Ok(dto), Problem);
    }

    [HttpGet(ApiEndpoints.V1.MyCompanyTransfers.GetBalance)]
    public async Task<IActionResult> GetBalance([FromQuery] string? currency, CancellationToken ct)
    {
        var agentId = User.FindFirstValue("agent_id");
        if (string.IsNullOrWhiteSpace(agentId))
            return Problem(AppErrors.Common.Unauthorized());
        var result = await _mediator.Send(new GetBalanceQuery(agentId!, currency), ct);
        Response.Headers.CacheControl = "no-store";
        return result.Match(dto => Ok(dto), Problem);
    }

    [HttpGet(ApiEndpoints.V1.MyCompanyTransfers.GetRate)]
    public async Task<IActionResult> Get([FromQuery] string baseCurrency, [FromQuery] string currency, CancellationToken ct)
    {
        var res = await _mediator.Send(new GetFxRateQuery(baseCurrency, currency), ct);
        return res.Match(Ok, Problem);
    }
}