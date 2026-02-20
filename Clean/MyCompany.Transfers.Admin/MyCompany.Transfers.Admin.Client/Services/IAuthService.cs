namespace MyCompany.Transfers.Admin.Client.Services;

/// <summary>
/// Авторизация: только логин через AD (токен сохраняется в localStorage).
/// Регистрация и хранение пользователей не используются.
/// </summary>
public interface IAuthService
{
    Task<string?> GetTokenAsync(CancellationToken ct = default);
    Task SetTokenAsync(string token, CancellationToken ct = default);
    Task ClearTokenAsync(CancellationToken ct = default);
    Task<bool> IsAuthenticatedAsync(CancellationToken ct = default);
    /// <summary>Возвращает true при успехе, иначе false (неверный логин/пароль или ошибка).</summary>
    Task<(bool success, string? error)> LoginAsync(string login, string password, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
}
