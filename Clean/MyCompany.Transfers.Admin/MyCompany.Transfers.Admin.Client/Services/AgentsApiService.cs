using System.Net;
using System.Net.Http.Json;
using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface IAgentsApiService
{
    Task<PagedResult<AgentAdminDto>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null, CancellationToken ct = default);
    Task<AgentAdminDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<(bool success, string? error)> CreateAsync(AgentAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> UpdateAsync(string id, AgentAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> DeleteAsync(string id, CancellationToken ct = default);
}

public sealed class AgentsApiService : IAgentsApiService
{
    private readonly IHttpClientFactory _httpFactory;

    public AgentsApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    public async Task<PagedResult<AgentAdminDto>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        var url = "api/admin/agents?" + string.Join("&", query);
        var result = await Api().GetFromJsonAsync<PagedResult<AgentAdminDto>>(url, ct);
        return result ?? new PagedResult<AgentAdminDto>();
    }

    public async Task<AgentAdminDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var response = await Api().GetAsync($"api/admin/agents/{Uri.EscapeDataString(id)}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AgentAdminDto>(ct);
    }

    public async Task<(bool success, string? error)> CreateAsync(AgentAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PostAsJsonAsync("api/admin/agents", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> UpdateAsync(string id, AgentAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PutAsJsonAsync($"api/admin/agents/{Uri.EscapeDataString(id)}", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> DeleteAsync(string id, CancellationToken ct = default)
    {
        var response = await Api().DeleteAsync($"api/admin/agents/{Uri.EscapeDataString(id)}", ct);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent) return (true, null);
        return (false, await ReadErrorAsync(response));
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return response.ReasonPhrase;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("message", out var m)) return m.GetString();
            if (root.TryGetProperty("title", out var t)) return t.GetString();
            if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0
                && root[0].TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch { }
        return body.Length > 200 ? body[..200] + "…" : body;
    }
}
