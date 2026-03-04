using System.Net.Http.Json;
using System.Text.Json;
using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface IDashboardApiService
{
    Task<DashboardOverviewModel> GetOverviewAsync(CancellationToken ct = default);
}

public sealed class DashboardApiService : IDashboardApiService
{
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DashboardApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    public async Task<DashboardOverviewModel> GetOverviewAsync(CancellationToken ct = default)
    {
        var response = await Api().GetAsync("api/admin/dashboard/overview", ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Загрузка дашборда не выполнена ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<DashboardOverviewModel>(JsonOptions, ct);
        return result ?? new DashboardOverviewModel();
    }
}

