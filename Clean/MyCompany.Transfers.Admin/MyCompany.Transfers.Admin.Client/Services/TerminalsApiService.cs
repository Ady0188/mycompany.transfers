using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface ITerminalsApiService
{
    Task<PagedResult<TerminalAdminDto>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null, CancellationToken ct = default);
    Task<TerminalAdminDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<(bool success, string? error)> CreateAsync(TerminalAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> UpdateAsync(string id, TerminalAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> DeleteAsync(string id, CancellationToken ct = default);
}

public sealed class TerminalsApiService : ITerminalsApiService
{
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TerminalsApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    public async Task<PagedResult<TerminalAdminDto>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        var url = "api/admin/terminals?" + string.Join("&", query);
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос списка терминалов не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TerminalAdminDto>>(JsonOptions, ct);
        return result ?? new PagedResult<TerminalAdminDto>();
    }

    public async Task<TerminalAdminDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var response = await Api().GetAsync($"api/admin/terminals/{Uri.EscapeDataString(id)}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TerminalAdminDto>(JsonOptions, ct);
    }

    public async Task<(bool success, string? error)> CreateAsync(TerminalAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PostAsJsonAsync("api/admin/terminals", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> UpdateAsync(string id, TerminalAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PutAsJsonAsync($"api/admin/terminals/{Uri.EscapeDataString(id)}", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> DeleteAsync(string id, CancellationToken ct = default)
    {
        var response = await Api().DeleteAsync($"api/admin/terminals/{Uri.EscapeDataString(id)}", ct);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }
}
