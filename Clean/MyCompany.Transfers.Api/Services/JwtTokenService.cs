using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace MyCompany.Transfers.Api.Services;

/// <summary>
/// Создаёт JWT с теми же параметрами, что и валидация (Admin:Jwt).
/// Роли берутся из Admin:AllowedRoles — в токен кладётся первая разрешённая роль.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(string userName, string login, IEnumerable<string> roles)
    {
        var jwtConfig = _config.GetSection("Admin:Jwt");
        var issuer = jwtConfig["Issuer"] ?? "";
        var audience = jwtConfig["Audience"] ?? "";
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(jwtConfig["Key"] ?? "");
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, login),
            new(ClaimTypes.Name, userName),
            new(JwtRegisteredClaimNames.Sub, login)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(8),
            creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
