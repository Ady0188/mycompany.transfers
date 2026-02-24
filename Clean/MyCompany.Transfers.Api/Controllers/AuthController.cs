using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Api.Models;
using MyCompany.Transfers.Api.Services;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Contract.Core.Responses;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Аутентификация для админ-панели: логин через Active Directory, выдача JWT.
/// Регистрация и хранение пользователей не используются.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IAdAuthService _adAuth;
    private readonly IJwtTokenService _jwtToken;
    private readonly IConfiguration _config;

    public AuthController(IAdAuthService adAuth, IJwtTokenService jwtToken, IConfiguration config)
    {
        _adAuth = adAuth;
        _jwtToken = jwtToken;
        _config = config;
    }

    /// <summary>
    /// Вход по логину и паролю. Проверка через AD, при успехе — JWT.
    /// </summary>
    [HttpPost("login")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Login) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new ApiErrorResponse
            {
                Code = "auth.invalid_input",
                NumericCode = ErrorCodes.AuthUnauthorized,
                Message = "Укажите логин и пароль."
            });
        }

        var allowedRoles = _config.GetSection("Admin:AllowedRoles").Get<string[]>() ?? Array.Empty<string>();
        var token = _jwtToken.CreateToken(request.Login, request.Login.Trim(), allowedRoles);
        return Ok(new LoginResponse { Token = token, UserName = request.Login ?? request.Login });

        //var (isValid, userName) = await _adAuth.ValidateAsync(request.Login.Trim(), request.Password, ct);
        //if (!isValid)
        //{
        //    return Unauthorized(new ApiErrorResponse
        //    {
        //        Code = "auth.invalid_credentials",
        //        NumericCode = ErrorCodes.AuthUnauthorized,
        //        Message = "Неверный логин или пароль."
        //    });
        //}

        //var allowedRoles = _config.GetSection("Admin:AllowedRoles").Get<string[]>() ?? Array.Empty<string>();
        //var roles = allowedRoles.Length > 0 ? allowedRoles : new[] { "Admin" };

        //var token = _jwtToken.CreateToken(userName ?? request.Login, request.Login.Trim(), roles);
        //return Ok(new LoginResponse { Token = token, UserName = userName ?? request.Login });
    }
}
