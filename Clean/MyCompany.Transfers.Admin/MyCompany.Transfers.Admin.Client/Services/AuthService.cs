using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace MyCompany.Transfers.Admin.Client.Services;

public sealed class AuthService : IAuthService
{
    private const string NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
    private const string StorageKey = "admin_jwt";
    private readonly IHttpClientFactory _httpFactory;
    private readonly IJSRuntime _js;

    public AuthService(IHttpClientFactory httpFactory, IJSRuntime js)
    {
        _httpFactory = httpFactory;
        _js = js;
    }

    public async Task<string?> GetTokenAsync(CancellationToken ct = default)
    {
        try
        {
            return await _js.InvokeAsync<string?>("localStorage.getItem", ct, StorageKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetTokenAsync(string token, CancellationToken ct = default)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", ct, StorageKey, token);
    }

    public async Task ClearTokenAsync(CancellationToken ct = default)
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", ct, StorageKey);
    }

    public async Task<bool> IsAuthenticatedAsync(CancellationToken ct = default)
    {
        var token = await GetTokenAsync(ct);
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task<string?> GetUserDisplayNameAsync(CancellationToken ct = default)
    {
        var token = await GetTokenAsync(ct);
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;
            var payload = parts[1];
            var base64 = payload.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty(NameClaimType, out var nameProp))
                return nameProp.GetString();
            if (root.TryGetProperty("name", out nameProp))
                return nameProp.GetString();
            if (root.TryGetProperty("unique_name", out nameProp))
                return nameProp.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool success, string? error)> LoginAsync(string login, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return (false, "Укажите логин и пароль.");

        try
        {
            // Логин без Bearer — используем клиент без handler с токеном или отдельный base URL
            var client = _httpFactory.CreateClient("Api");
            var body = new { Login = login.Trim(), Password = password };
            var response = await client.PostAsJsonAsync("api/auth/login", body, ct);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var token = root.TryGetProperty("token", out var t) ? t.GetString() : null;
                if (string.IsNullOrEmpty(token))
                    return (false, "Некорректный ответ сервера.");
                await SetTokenAsync(token, ct);
                return (true, null);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var problem = await response.Content.ReadAsStringAsync(ct);
                try
                {
                    using var doc = JsonDocument.Parse(problem);
                    var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : null;
                    return (false, msg ?? "Неверный логин или пароль.");
                }
                catch
                {
                    return (false, "Неверный логин или пароль.");
                }
            }

            return (false, "Ошибка сервера. Попробуйте позже.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        await ClearTokenAsync(ct);
    }
}
