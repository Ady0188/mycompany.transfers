using System.Net.Http.Json;
using System.Text.Json;
using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface IFxRatesApiService
{
    Task<List<FxRateAdminDto>> GetAllAsync(string? agentId = null, CancellationToken ct = default);
}

public sealed class FxRatesApiService : IFxRatesApiService
{
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public FxRatesApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    public async Task<List<FxRateAdminDto>> GetAllAsync(string? agentId = null, CancellationToken ct = default)
    {
        var url = "api/admin/fx-rates";
        if (!string.IsNullOrWhiteSpace(agentId))
            url += "?agentId=" + Uri.EscapeDataString(agentId);
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос курсов валют не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<List<FxRateAdminDto>>(JsonOptions, ct);
        return result ?? new List<FxRateAdminDto>();
    }
}
