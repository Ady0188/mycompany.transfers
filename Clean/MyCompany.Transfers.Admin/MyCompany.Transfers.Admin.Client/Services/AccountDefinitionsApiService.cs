using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface IAccountDefinitionsApiService
{
    Task<List<AccountDefinitionAdminDto>> GetAllAsync(CancellationToken ct = default);
    Task<AccountDefinitionAdminDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(bool success, string? error)> CreateAsync(AccountDefinitionAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> UpdateAsync(Guid id, AccountDefinitionAdminDto dto, CancellationToken ct = default);
    Task<(bool success, string? error)> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class AccountDefinitionsApiService : IAccountDefinitionsApiService
{
    private readonly IHttpClientFactory _httpFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AccountDefinitionsApiService(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    private HttpClient Api() => _httpFactory.CreateClient("Api");

    public async Task<List<AccountDefinitionAdminDto>> GetAllAsync(CancellationToken ct = default)
    {
        var response = await Api().GetAsync("api/admin/account-definitions", ct);
        if (!response.IsSuccessStatusCode)
        {
            var msg = await ApiErrorHelper.ReadErrorAsync(response);
            throw new HttpRequestException($"Запрос списка определений счёта не выполнен ({(int)response.StatusCode}): {msg ?? response.ReasonPhrase}");
        }
        var result = await response.Content.ReadFromJsonAsync<List<AccountDefinitionAdminDto>>(JsonOptions, ct);
        return result ?? new List<AccountDefinitionAdminDto>();
    }

    public async Task<AccountDefinitionAdminDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await Api().GetAsync($"api/admin/account-definitions/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AccountDefinitionAdminDto>(JsonOptions, ct);
    }

    public async Task<(bool success, string? error)> CreateAsync(AccountDefinitionAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PostAsJsonAsync("api/admin/account-definitions", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> UpdateAsync(Guid id, AccountDefinitionAdminDto dto, CancellationToken ct = default)
    {
        var response = await Api().PutAsJsonAsync($"api/admin/account-definitions/{id}", dto, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }

    public async Task<(bool success, string? error)> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await Api().DeleteAsync($"api/admin/account-definitions/{id}", ct);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent) return (true, null);
        return (false, await ApiErrorHelper.ReadErrorAsync(response));
    }
}
