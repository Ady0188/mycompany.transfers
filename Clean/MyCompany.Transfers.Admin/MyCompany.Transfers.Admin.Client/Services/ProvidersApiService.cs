using System.Net;
using System.Net.Http.Json;
using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface IProvidersApiService
{
    Task<List<ProviderAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<ProviderAdminDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<(bool success, string? error)> CreateAsync(ProviderAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> UpdateAsync(string id, ProviderAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> DeleteAsync(string id, CancellationToken ct = default);
}

public sealed class ProvidersApiService : IProvidersApiService
{
    private readonly IHttpClientFactory _httpFactory;

    public ProvidersApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    public async Task<List<ProviderAdminDto>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await Api().GetFromJsonAsync<List<ProviderAdminDto>>("api/admin/providers", ct);
        return list ?? new List<ProviderAdminDto>();
    }

    public async Task<ProviderAdminDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var response = await Api().GetAsync($"api/admin/providers/{Uri.EscapeDataString(id)}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProviderAdminDto>(ct);
    }

    public async Task<(bool success, string? error)> CreateAsync(ProviderAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PostAsJsonAsync("api/admin/providers", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> UpdateAsync(string id, ProviderAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PutAsJsonAsync($"api/admin/providers/{Uri.EscapeDataString(id)}", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> DeleteAsync(string id, CancellationToken ct = default)
    {
        var response = await Api().DeleteAsync($"api/admin/providers/{Uri.EscapeDataString(id)}", ct);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }
}

internal static class ApiErrorHelper
{
    public static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
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
