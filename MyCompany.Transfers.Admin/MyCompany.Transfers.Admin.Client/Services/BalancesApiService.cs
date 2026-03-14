using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface IBalancesApiService
{
    Task<PagedResult<DailyBalanceItemModel>> GetDailyBalancesAsync(DailyBalanceFilterModel filter, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<BalanceHistoryItemModel>> GetBalanceHistoryAsync(BalanceHistoryFilterModel filter, int page, int pageSize, CancellationToken ct = default);
}

public sealed class BalancesApiService : IBalancesApiService
{
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    public BalancesApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    public async Task<PagedResult<DailyBalanceItemModel>> GetDailyBalancesAsync(DailyBalanceFilterModel filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (filter.From.HasValue)
        {
            var from = new DateTimeOffset(filter.From.Value, TimeSpan.Zero);
            query.Add($"from={Uri.EscapeDataString(from.ToString("O"))}");
        }
        if (filter.To.HasValue)
        {
            var to = new DateTimeOffset(filter.To.Value, TimeSpan.Zero);
            query.Add($"to={Uri.EscapeDataString(to.ToString("O"))}");
        }
        if (!string.IsNullOrWhiteSpace(filter.AgentId))
            query.Add($"agentId={Uri.EscapeDataString(filter.AgentId.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.TerminalId))
            query.Add($"terminalId={Uri.EscapeDataString(filter.TerminalId.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.Currency))
            query.Add($"currency={Uri.EscapeDataString(filter.Currency.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.TimeZoneId))
            query.Add($"timeZoneId={Uri.EscapeDataString(filter.TimeZoneId.Trim())}");
        if (filter.Scope.HasValue)
            query.Add($"scope={Uri.EscapeDataString(filter.Scope.Value.ToString())}");

        var url = "api/admin/balances/daily?" + string.Join("&", query);
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос дневных балансов не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }

        var result = await response.Content.ReadFromJsonAsync<PagedResult<DailyBalanceItemModel>>(JsonOptions, ct);
        return result ?? new PagedResult<DailyBalanceItemModel>();
    }

    public async Task<PagedResult<BalanceHistoryItemModel>> GetBalanceHistoryAsync(BalanceHistoryFilterModel filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (filter.From.HasValue)
        {
            var from = new DateTimeOffset(filter.From.Value, TimeSpan.Zero);
            query.Add($"from={Uri.EscapeDataString(from.ToString("O"))}");
        }
        if (filter.To.HasValue)
        {
            var to = new DateTimeOffset(filter.To.Value, TimeSpan.Zero);
            query.Add($"to={Uri.EscapeDataString(to.ToString("O"))}");
        }
        if (!string.IsNullOrWhiteSpace(filter.AgentId))
            query.Add($"agentId={Uri.EscapeDataString(filter.AgentId.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.TerminalId))
            query.Add($"terminalId={Uri.EscapeDataString(filter.TerminalId.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.Currency))
            query.Add($"currency={Uri.EscapeDataString(filter.Currency.Trim())}");

        var url = "api/admin/balances/history?" + string.Join("&", query);
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос истории балансов не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }

        var result = await response.Content.ReadFromJsonAsync<PagedResult<BalanceHistoryItemModel>>(JsonOptions, ct);
        return result ?? new PagedResult<BalanceHistoryItemModel>();
    }
}

