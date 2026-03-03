using System.Security.Claims;

namespace MyCompany.Transfers.Api.Services;

/// <summary>
/// Выпуск JWT для успешно аутентифицированного пользователя.
/// </summary>
public interface IJwtTokenService
{
    string CreateToken(string userName, string login, IEnumerable<string> roles);
}
