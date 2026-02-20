namespace MyCompany.Transfers.Api.Services;

/// <summary>
/// Проверка учётных данных пользователя в Active Directory.
/// </summary>
public interface IAdAuthService
{
    /// <summary>
    /// Проверяет логин и пароль в AD. Возвращает имя пользователя при успехе.
    /// </summary>
    Task<(bool isValid, string? userName)> ValidateAsync(string login, string password, CancellationToken ct = default);
}
