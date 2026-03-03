using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface IAgentCurrencyAccessApiService
{
    Task<List<AgentCurrencyAccessAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<AgentCurrencyAccessAdminDto?> GetByKeyAsync(string agentId, string currency, CancellationToken ct = default);
    Task<(bool success, string? error)> CreateAsync(AgentCurrencyAccessAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> UpdateAsync(string agentId, string currency, AgentCurrencyAccessAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> DeleteAsync(string agentId, string currency, CancellationToken ct = default);
}

public sealed class AgentCurrencyAccessApiService : IAgentCurrencyAccessApiService
{
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AgentCurrencyAccessApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    public async Task<List<AgentCurrencyAccessAdminDto>> GetAllAsync(CancellationToken ct = default)
    {
        var response = await Api().GetAsync("api/admin/access/currencies", ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос списка доступов (валюты) не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<List<AgentCurrencyAccessAdminDto>>(JsonOptions, ct);
        return result ?? new List<AgentCurrencyAccessAdminDto>();
    }

    public async Task<AgentCurrencyAccessAdminDto?> GetByKeyAsync(string agentId, string currency, CancellationToken ct = default)
    {
        var url = $"api/admin/access/currencies/{Uri.EscapeDataString(agentId)}/{Uri.EscapeDataString(currency)}";
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AgentCurrencyAccessAdminDto>(JsonOptions, ct);
    }

    public async Task<(bool success, string? error)> CreateAsync(AgentCurrencyAccessAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PostAsJsonAsync("api/admin/access/currencies", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> UpdateAsync(string agentId, string currency, AgentCurrencyAccessAdminDto dto, CancellationToken ct = default)
    {
        var url = $"api/admin/access/currencies/{Uri.EscapeDataString(agentId)}/{Uri.EscapeDataString(currency)}";
        var response = await Api().PutAsJsonAsync(url, dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> DeleteAsync(string agentId, string currency, CancellationToken ct = default)
    {
        var url = $"api/admin/access/currencies/{Uri.EscapeDataString(agentId)}/{Uri.EscapeDataString(currency)}";
        var response = await Api().DeleteAsync(url, ct);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }
}
