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
    Task<(byte[] Content, string FileName)> GetExportFileAsync(TransfersReportType reportType, TransfersReportFilterModel filter, string format, CancellationToken ct = default);
    Task<TransfersReportResultModel<TransfersByBankReportItemModel>> GetTransfersByBankCardsAsync(TransfersReportFilterModel filter, int page, int pageSize, CancellationToken ct = default);
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
        if (!string.IsNullOrWhiteSpace(filter.ProviderId))
            q.Add($"providerId={Uri.EscapeDataString(filter.ProviderId.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.ServiceId))
            q.Add($"serviceId={Uri.EscapeDataString(filter.ServiceId.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.AmountCurrency))
            q.Add($"amountCurrency={Uri.EscapeDataString(filter.AmountCurrency.Trim())}");
        return string.Join("&", q);
    }

    private static string BuildExportQuery(TransfersReportFilterModel filter, string format)
    {
        var q = new List<string> { $"format={Uri.EscapeDataString(format)}" };
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
        if (!string.IsNullOrWhiteSpace(filter.ProviderId))
            q.Add($"providerId={Uri.EscapeDataString(filter.ProviderId.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.ServiceId))
            q.Add($"serviceId={Uri.EscapeDataString(filter.ServiceId.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.AmountCurrency))
            q.Add($"amountCurrency={Uri.EscapeDataString(filter.AmountCurrency.Trim())}");
        if (filter.GroupBy == TransfersReportGroupByClient.Month)
            q.Add("groupBy=Month");
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

    public async Task<TransfersReportResultModel<TransfersByBankReportItemModel>> GetTransfersByBankCardsAsync(TransfersReportFilterModel filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = BuildCommonQuery(filter, page, pageSize);
        var url = "api/admin/reports/transfers/by-bank-cards?" + query;
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос отчета (переводы по банкам, карты IPS/FIMI) не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<TransfersReportResultModel<TransfersByBankReportItemModel>>(JsonOptions, ct);
        return result ?? new TransfersReportResultModel<TransfersByBankReportItemModel>();
    }

    public async Task<(byte[] Content, string FileName)> GetExportFileAsync(TransfersReportType reportType, TransfersReportFilterModel filter, string format, CancellationToken ct = default)
    {
        var path = reportType switch
        {
            TransfersReportType.ByPeriod => "by-period/export",
            TransfersReportType.ByAgent => "by-agent/export",
            TransfersReportType.ByProvider => "by-provider/export",
            TransfersReportType.Revenue => "revenue/export",
            _ => "by-period/export"
        };
        var query = BuildExportQuery(filter, format);
        var url = $"api/admin/reports/transfers/{path}?{query}";
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Экспорт не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName ?? $"report.{format}";
        fileName = fileName.Trim('"');
        return (bytes, fileName);
    }
}

