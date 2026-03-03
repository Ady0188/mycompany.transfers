using System.Net.Http.Json;
using System.Text.Json;
using MyCompany.Transfers.Admin.Client.Models.Reports;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface IReportsApiService
{
    Task<TransfersReportResultModel<TransfersByPeriodReportItemModel>> GetTransfersByPeriodAsync(TransfersReportFilterModel filter, int page, int pageSize, CancellationToken ct = default);
    Task<TransfersReportResultModel<TransfersByAgentReportItemModel>> GetTransfersByAgentAsync(TransfersReportFilterModel filter, int page, int pageSize, CancellationToken ct = default);
    Task<TransfersReportResultModel<TransfersByProviderReportItemModel>> GetTransfersByProviderAsync(TransfersReportFilterModel filter, int page, int pageSize, CancellationToken ct = default);
    Task<TransfersReportResultModel<TransfersRevenueReportItemModel>> GetTransfersRevenueAsync(TransfersReportFilterModel filter, int page, int pageSize, CancellationToken ct = default);
}

public sealed class ReportsApiService : IReportsApiService
{
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ReportsApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    private static string BuildCommonQuery(TransfersReportFilterModel filter, int page, int pageSize)
    {
        var q = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (filter.From.HasValue)
        {
            var from = new DateTimeOffset(filter.From.Value, TimeSpan.Zero);
            q.Add($"from={Uri.EscapeDataString(from.ToString("O"))}");
        }
        if (filter.To.HasValue)
        {
            var to = new DateTimeOffset(filter.To.Value, TimeSpan.Zero);
            q.Add($"to={Uri.EscapeDataString(to.ToString("O"))}");
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
            q.Add($"status={Uri.EscapeDataString(filter.Status.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.AgentId))
            q.Add($"agentId={Uri.EscapeDataString(filter.AgentId.Trim())}");

        return string.Join("&", q);
    }

    public async Task<TransfersReportResultModel<TransfersByPeriodReportItemModel>> GetTransfersByPeriodAsync(TransfersReportFilterModel filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BuildCommonQuery(filter, page, pageSize);
        query += $"&groupBy={filter.GroupBy}";
        var url = "api/admin/reports/transfers/by-period?" + query;
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос отчета (переводы по периодам) не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<TransfersReportResultModel<TransfersByPeriodReportItemModel>>(JsonOptions, ct);
        return result ?? new TransfersReportResultModel<TransfersByPeriodReportItemModel>();
    }

    public async Task<TransfersReportResultModel<TransfersByAgentReportItemModel>> GetTransfersByAgentAsync(TransfersReportFilterModel filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BuildCommonQuery(filter, page, pageSize);
        var url = "api/admin/reports/transfers/by-agent?" + query;
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос отчета (переводы по агентам) не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<TransfersReportResultModel<TransfersByAgentReportItemModel>>(JsonOptions, ct);
        return result ?? new TransfersReportResultModel<TransfersByAgentReportItemModel>();
    }

    public async Task<TransfersReportResultModel<TransfersByProviderReportItemModel>> GetTransfersByProviderAsync(TransfersReportFilterModel filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BuildCommonQuery(filter, page, pageSize);
        var url = "api/admin/reports/transfers/by-provider?" + query;
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос отчета (переводы по провайдерам) не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<TransfersReportResultModel<TransfersByProviderReportItemModel>>(JsonOptions, ct);
        return result ?? new TransfersReportResultModel<TransfersByProviderReportItemModel>();
    }

    public async Task<TransfersReportResultModel<TransfersRevenueReportItemModel>> GetTransfersRevenueAsync(TransfersReportFilterModel filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BuildCommonQuery(filter, page, pageSize);
        query += $"&groupBy={filter.GroupBy}";
        var url = "api/admin/reports/transfers/revenue?" + query;
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос отчета (доходность/комиссии) не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<TransfersReportResultModel<TransfersRevenueReportItemModel>>(JsonOptions, ct);
        return result ?? new TransfersReportResultModel<TransfersRevenueReportItemModel>();
    }
}

