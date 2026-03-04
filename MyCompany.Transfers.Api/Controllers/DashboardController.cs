using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Api.Auth;
using MyCompany.Transfers.Application.Reports.Transfers;

namespace MyCompany.Transfers.Api.Controllers;

public sealed class DashboardOverviewDto
{
    public long TransfersToday { get; init; }
    public long TransfersLast7Days { get; init; }
    public long RevenueLast7DaysMinor { get; init; }
    public string RevenueCurrency { get; init; } = "";
    public IReadOnlyList<string> Last14DaysLabels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<long> Last14DaysTransfers { get; init; } = Array.Empty<long>();
    public IReadOnlyList<long> Last14DaysRevenueMinor { get; init; } = Array.Empty<long>();
    public IReadOnlyList<string> TopProvidersLabels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<long> TopProvidersCounts { get; init; } = Array.Empty<long>();
}

/// <summary>
/// Дашборд для главной страницы админ-панели.
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class DashboardController : BaseController
{
    private readonly ISender _mediator;

    public DashboardController(ISender mediator) => _mediator = mediator;

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var today = now.Date;
        var from14 = today.AddDays(-13);
        var from7 = today.AddDays(-6);

        var filter14 = new TransfersCommonReportFilter(from14, now, null, null, null, null, null);
        var filter7 = new TransfersCommonReportFilter(from7, now, null, null, null, null, null);

        var byPeriodTask = _mediator.Send(
            new GetTransfersByPeriodReportQuery(filter14, TransfersReportGroupBy.Day, 1, TransfersReportLimits.ExportMaxRows),
            ct);
        var revenueTask = _mediator.Send(
            new GetTransfersRevenueReportQuery(filter14, TransfersReportGroupBy.Day, 1, TransfersReportLimits.ExportMaxRows),
            ct);
        var byProviderTask = _mediator.Send(
            new GetTransfersByProviderReportQuery(filter7, 1, 20),
            ct);

        await Task.WhenAll(byPeriodTask, revenueTask, byProviderTask);

        var byPeriod = await byPeriodTask;
        var revenue = await revenueTask;
        var byProvider = await byProviderTask;

        var periodItems = byPeriod.Items.OrderBy(i => i.PeriodStart).ToList();
        var revenueItems = revenue.Items.OrderBy(i => i.PeriodStart).ToList();

        var labels = periodItems
            .Select(i => i.PeriodStart.ToString("MM-dd"))
            .ToList();
        var transfersSeries = periodItems
            .Select(i => i.TransfersCount)
            .ToList();
        var revenueSeries = revenueItems
            .Select(i => i.MarginMinor)
            .ToList();

        var from7Date = today.AddDays(-6);
        var transfersLast7 = periodItems
            .Where(i => i.PeriodStart.Date >= from7Date)
            .Sum(i => i.TransfersCount);
        var revenueLast7Minor = revenueItems
            .Where(i => i.PeriodStart.Date >= from7Date)
            .Sum(i => i.MarginMinor);
        var revenueCurrency = revenueItems.FirstOrDefault()?.Currency ?? "TJS";

        var transfersToday = periodItems
            .Where(i => i.PeriodStart.Date == today)
            .Sum(i => i.TransfersCount);

        var topProviders = byProvider.Items
            .OrderByDescending(i => i.TransfersCount)
            .Take(5)
            .ToList();

        var dto = new DashboardOverviewDto
        {
            TransfersToday = transfersToday,
            TransfersLast7Days = transfersLast7,
            RevenueLast7DaysMinor = revenueLast7Minor,
            RevenueCurrency = revenueCurrency,
            Last14DaysLabels = labels,
            Last14DaysTransfers = transfersSeries,
            Last14DaysRevenueMinor = revenueSeries,
            TopProvidersLabels = topProviders.Select(p => p.ProviderName ?? p.ProviderId).ToList(),
            TopProvidersCounts = topProviders.Select(p => p.TransfersCount).ToList()
        };

        return Ok(dto);
    }
}

