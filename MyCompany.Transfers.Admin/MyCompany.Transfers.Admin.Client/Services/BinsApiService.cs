using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface IBinsApiService
{
    Task<PagedResult<BinAdminDto>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null, CancellationToken ct = default);
    Task<BinAdminDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(bool success, string? error)> CreateAsync(BinAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> UpdateAsync(Guid id, BinAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class BinsApiService : IBinsApiService
{
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public BinsApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    public async Task<PagedResult<BinAdminDto>> GetPagedAsync(int page = 1, int pageSize = 10, string? search = null, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        var url = "api/admin/bins?" + string.Join("&", query);
        var response = await Api().GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос списка БИН не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<PagedResult<BinAdminDto>>(JsonOptions, ct);
        return result ?? new PagedResult<BinAdminDto>();
    }

    public async Task<BinAdminDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await Api().GetAsync($"api/admin/bins/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<BinAdminDto>(JsonOptions, ct);
    }

    public async Task<(bool success, string? error)> CreateAsync(BinAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PostAsJsonAsync("api/admin/bins", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> UpdateAsync(Guid id, BinAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PutAsJsonAsync($"api/admin/bins/{id}", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await Api().DeleteAsync($"api/admin/bins/{id}", ct);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }
}
