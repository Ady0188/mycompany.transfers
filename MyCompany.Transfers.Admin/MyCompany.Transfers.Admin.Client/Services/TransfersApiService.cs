using System.Net.Http.Json;
using System.Text.Json;
using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public sealed class TransfersApiService : ITransfersApiService
{
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TransfersApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    public async Task<PagedResult<TransferAdminDto>> GetPagedAsync(int page = 1, int pageSize = 10, TransfersFilter? filter = null, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Id) && Guid.TryParse(filter.Id.Trim(), out var idVal))
                query.Add($"id={Uri.EscapeDataString(idVal.ToString())}");
            if (!string.IsNullOrWhiteSpace(filter.AgentId))
                query.Add($"agentId={Uri.EscapeDataString(filter.AgentId.Trim())}");
            if (!string.IsNullOrWhiteSpace(filter.ExternalId))
                query.Add($"externalId={Uri.EscapeDataString(filter.ExternalId.Trim())}");
            if (!string.IsNullOrWhiteSpace(filter.ProviderId))
                query.Add($"providerId={Uri.EscapeDataString(filter.ProviderId.Trim())}");
            if (!string.IsNullOrWhiteSpace(filter.ServiceId))
                query.Add($"serviceId={Uri.EscapeDataString(filter.ServiceId.Trim())}");
            if (!string.IsNullOrWhiteSpace(filter.Status))
                query.Add($"status={Uri.EscapeDataString(filter.Status.Trim())}");
            if (filter.CreatedFrom.HasValue)
                query.Add($"createdFrom={Uri.EscapeDataString(filter.CreatedFrom.Value.ToString("O"))}");
            if (filter.CreatedTo.HasValue)
                query.Add($"createdTo={Uri.EscapeDataString(filter.CreatedTo.Value.ToString("O"))}");
            if (!string.IsNullOrWhiteSpace(filter.Account))
                query.Add($"account={Uri.EscapeDataString(filter.Account.Trim())}");
        }
        var url = "api/admin/transfers?" + string.Join("&", query);
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос списка переводов не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TransferAdminDto>>(JsonOptions, ct);
        return result ?? new PagedResult<TransferAdminDto>();
    }
}
