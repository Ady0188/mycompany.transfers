using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface IAgentServiceAccessApiService
{
    Task<List<AgentServiceAccessAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<AgentServiceAccessAdminDto?> GetByKeyAsync(string agentId, string serviceId, CancellationToken ct = default);
    Task<(bool success, string? error)> CreateAsync(AgentServiceAccessAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> UpdateAsync(string agentId, string serviceId, AgentServiceAccessAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> DeleteAsync(string agentId, string serviceId, CancellationToken ct = default);
}

public sealed class AgentServiceAccessApiService : IAgentServiceAccessApiService
{
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AgentServiceAccessApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    public async Task<List<AgentServiceAccessAdminDto>> GetAllAsync(CancellationToken ct = default)
    {
        var response = await Api().GetAsync("api/admin/access/services", ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос списка доступов (услуги) не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<List<AgentServiceAccessAdminDto>>(JsonOptions, ct);
        return result ?? new List<AgentServiceAccessAdminDto>();
    }

    public async Task<AgentServiceAccessAdminDto?> GetByKeyAsync(string agentId, string serviceId, CancellationToken ct = default)
    {
        var url = $"api/admin/access/services/{Uri.EscapeDataString(agentId)}/{Uri.EscapeDataString(serviceId)}";
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AgentServiceAccessAdminDto>(JsonOptions, ct);
    }

    public async Task<(bool success, string? error)> CreateAsync(AgentServiceAccessAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PostAsJsonAsync("api/admin/access/services", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> UpdateAsync(string agentId, string serviceId, AgentServiceAccessAdminDto dto, CancellationToken ct = default)
    {
        var url = $"api/admin/access/services/{Uri.EscapeDataString(agentId)}/{Uri.EscapeDataString(serviceId)}";
        var response = await Api().PutAsJsonAsync(url, dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> DeleteAsync(string agentId, string serviceId, CancellationToken ct = default)
    {
        var url = $"api/admin/access/services/{Uri.EscapeDataString(agentId)}/{Uri.EscapeDataString(serviceId)}";
        var response = await Api().DeleteAsync(url, ct);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }
}
