using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Api.Auth;
using MyCompany.Transfers.Application.Reports.Transfers;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Отчеты по переводам для админ-панели.
/// </summary>
[ApiController]
[Route("api/admin/reports/transfers")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class ReportsController : BaseController
{
    private readonly ISender _mediator;

    public ReportsController(ISender mediator) => _mediator = mediator;

    [HttpGet("by-period")]
    public async Task<IActionResult> GetByPeriod(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] TransfersReportGroupBy groupBy = TransfersReportGroupBy.Day,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new TransfersCommonReportFilter(from, to, status, agentId);
        var result = await _mediator.Send(new GetTransfersByPeriodReportQuery(filter, groupBy, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("by-agent")]
    public async Task<IActionResult> GetByAgent(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new TransfersCommonReportFilter(from, to, status, agentId);
        var result = await _mediator.Send(new GetTransfersByAgentReportQuery(filter, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("by-provider")]
    public async Task<IActionResult> GetByProvider(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new TransfersCommonReportFilter(from, to, status, agentId);
        var result = await _mediator.Send(new GetTransfersByProviderReportQuery(filter, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] TransfersReportGroupBy groupBy = TransfersReportGroupBy.Day,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = new TransfersCommonReportFilter(from, to, status, agentId);
        var result = await _mediator.Send(new GetTransfersRevenueReportQuery(filter, groupBy, page, pageSize), ct);
        return Ok(result);
    }
}

