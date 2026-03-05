using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Api.Auth;
using MyCompany.Transfers.Api.Reports;
using MyCompany.Transfers.Application.Reports.Transfers;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Отчеты по переводам для админ-панели.
/// </summary>
[ApiController]
[Route("api/admin/reports/transfers")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json", "text/csv", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class ReportsController : BaseController
{
    private readonly ISender _mediator;

    public ReportsController(ISender mediator) => _mediator = mediator;

    private static TransfersCommonReportFilter BuildFilter(
        DateTimeOffset? from, DateTimeOffset? to, string? status, string? agentId, string? providerId, string? serviceId, string? amountCurrency) =>
        new(from, to, status, agentId, providerId, serviceId, amountCurrency);

    private static IActionResult? ValidatePeriod(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (!from.HasValue || !to.HasValue) return null;
        var days = (to.Value - from.Value).TotalDays;
        if (days < 0)
            return new BadRequestObjectResult(new { error = "Дата «По» должна быть не раньше даты «С»." });
        if (days > TransfersReportLimits.MaxPeriodDays)
            return new BadRequestObjectResult(new { error = $"Период не должен превышать {TransfersReportLimits.MaxPeriodDays} дней." });
        return null;
    }

    [HttpGet("by-period")]
    public async Task<IActionResult> GetByPeriod(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] string? providerId,
        [FromQuery] string? serviceId,
        [FromQuery] string? amountCurrency,
        [FromQuery] TransfersReportGroupBy groupBy = TransfersReportGroupBy.Day,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (ValidatePeriod(from, to) is { } err) return err;
        var filter = BuildFilter(from, to, status, agentId, providerId, serviceId, amountCurrency);
        var result = await _mediator.Send(new GetTransfersByPeriodReportQuery(filter, groupBy, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("by-period/export")]
    public async Task<IActionResult> ExportByPeriod(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] string? providerId,
        [FromQuery] string? serviceId,
        [FromQuery] string? amountCurrency,
        [FromQuery] TransfersReportGroupBy groupBy = TransfersReportGroupBy.Day,
        [FromQuery] string format = "csv",
        CancellationToken ct = default)
    {
        if (ValidatePeriod(from, to) is { } err) return err;
        var filter = BuildFilter(from, to, status, agentId, providerId, serviceId, amountCurrency);
        var result = await _mediator.Send(new GetTransfersByPeriodReportQuery(filter, groupBy, 1, TransfersReportLimits.ExportMaxRows), ct);
        var (content, contentType, fileName) = format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? TransfersReportExportHelper.ToExcel(result)
            : TransfersReportExportHelper.ToCsv(result);
        return File(content, contentType, fileName);
    }

    [HttpGet("by-agent")]
    public async Task<IActionResult> GetByAgent(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] string? providerId,
        [FromQuery] string? serviceId,
        [FromQuery] string? amountCurrency,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (ValidatePeriod(from, to) is { } err) return err;
        var filter = BuildFilter(from, to, status, agentId, providerId, serviceId, amountCurrency);
        var result = await _mediator.Send(new GetTransfersByAgentReportQuery(filter, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("by-agent/export")]
    public async Task<IActionResult> ExportByAgent(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] string? providerId,
        [FromQuery] string? serviceId,
        [FromQuery] string? amountCurrency,
        [FromQuery] string format = "csv",
        CancellationToken ct = default)
    {
        if (ValidatePeriod(from, to) is { } err) return err;
        var filter = BuildFilter(from, to, status, agentId, providerId, serviceId, amountCurrency);
        var result = await _mediator.Send(new GetTransfersByAgentReportQuery(filter, 1, TransfersReportLimits.ExportMaxRows), ct);
        var (content, contentType, fileName) = format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? TransfersReportExportHelper.ToExcel(result)
            : TransfersReportExportHelper.ToCsv(result);
        return File(content, contentType, fileName);
    }

    [HttpGet("by-provider")]
    public async Task<IActionResult> GetByProvider(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] string? providerId,
        [FromQuery] string? serviceId,
        [FromQuery] string? amountCurrency,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (ValidatePeriod(from, to) is { } err) return err;
        var filter = BuildFilter(from, to, status, agentId, providerId, serviceId, amountCurrency);
        var result = await _mediator.Send(new GetTransfersByProviderReportQuery(filter, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("by-provider/export")]
    public async Task<IActionResult> ExportByProvider(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] string? providerId,
        [FromQuery] string? serviceId,
        [FromQuery] string? amountCurrency,
        [FromQuery] string format = "csv",
        CancellationToken ct = default)
    {
        if (ValidatePeriod(from, to) is { } err) return err;
        var filter = BuildFilter(from, to, status, agentId, providerId, serviceId, amountCurrency);
        var result = await _mediator.Send(new GetTransfersByProviderReportQuery(filter, 1, TransfersReportLimits.ExportMaxRows), ct);
        var (content, contentType, fileName) = format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? TransfersReportExportHelper.ToExcel(result)
            : TransfersReportExportHelper.ToCsv(result);
        return File(content, contentType, fileName);
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] string? providerId,
        [FromQuery] string? serviceId,
        [FromQuery] string? amountCurrency,
        [FromQuery] TransfersReportGroupBy groupBy = TransfersReportGroupBy.Day,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (ValidatePeriod(from, to) is { } err) return err;
        var filter = BuildFilter(from, to, status, agentId, providerId, serviceId, amountCurrency);
        var result = await _mediator.Send(new GetTransfersRevenueReportQuery(filter, groupBy, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("revenue/export")]
    public async Task<IActionResult> ExportRevenue(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? status,
        [FromQuery] string? agentId,
        [FromQuery] string? providerId,
        [FromQuery] string? serviceId,
        [FromQuery] string? amountCurrency,
        [FromQuery] TransfersReportGroupBy groupBy = TransfersReportGroupBy.Day,
        [FromQuery] string format = "csv",
        CancellationToken ct = default)
    {
        if (ValidatePeriod(from, to) is { } err) return err;
        var filter = BuildFilter(from, to, status, agentId, providerId, serviceId, amountCurrency);
        var result = await _mediator.Send(new GetTransfersRevenueReportQuery(filter, groupBy, 1, TransfersReportLimits.ExportMaxRows), ct);
        var (content, contentType, fileName) = format.Equals("xlsx", StringComparison.OrdinalIgnoreCase)
            ? TransfersReportExportHelper.ToExcel(result)
            : TransfersReportExportHelper.ToCsv(result);
        return File(content, contentType, fileName);
    }

    /// <summary>Отчет по переводам внутри Таджикистана по картам (IPS, FIMI) в разрезе банков.</summary>
    [HttpGet("by-bank-cards")]
    public async Task<IActionResult> GetByBankCards(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? agentId,
        [FromQuery] string? amountCurrency,
        [FromQuery] string? providerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (ValidatePeriod(from, to) is { } err) return err;
        var filter = new TransfersCommonReportFilter(from, to, null, agentId, providerId, null, amountCurrency);
        var result = await _mediator.Send(new GetTransfersByBankReportQuery(filter, page, pageSize), ct);
        return Ok(result);
    }
}
