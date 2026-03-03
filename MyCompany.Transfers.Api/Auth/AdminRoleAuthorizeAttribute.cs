using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Contract.Core.Responses;
using System.Security.Claims;

namespace MyCompany.Transfers.Api.Auth;

/// <summary>
/// Авторизация для административных эндпоинтов по JWT Bearer токену.
/// Клиент (или отдельный сервис) аутентифицирует пользователя через AD и передаёт токен
/// с данными пользователя и ролями в заголовке Authorization: Bearer &lt;token&gt;.
/// Роли в токене должны совпадать с Admin:AllowedRoles или с ролями, указанными в атрибуте.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AdminRoleAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    /// <summary>
    /// Создаёт атрибут авторизации с указанными ролями.
    /// Если роли не указаны, используется значение из конфига Admin:AllowedRoles.
    /// </summary>
    public AdminRoleAuthorizeAttribute(params string[] roles)
    {
        _allowedRoles = roles;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
            {
                Code = "admin.token_required",
                NumericCode = ErrorCodes.AuthUnauthorized,
                Message = "Требуется действительный JWT в заголовке Authorization: Bearer <token>."
            });
            return;
        }

        var config = context.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var rolesToCheck = _allowedRoles.Length > 0
            ? _allowedRoles
            : config.GetSection("Admin:AllowedRoles").Get<string[]>() ?? Array.Empty<string>();

        if (rolesToCheck.Length == 0)
        {
            await Task.CompletedTask;
            return;
        }

        // Роли из claims: стандартные (ClaimTypes.Role) и часто используемые имена
        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role ||
                        c.Type == "role" ||
                        c.Type == "roles" ||
                        c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .SelectMany(c => c.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(r => r.Trim())
            .Where(r => r.Length > 0)
            .ToList();

        var hasRequiredRole = rolesToCheck.Any(role =>
            userRoles.Any(userRole => string.Equals(userRole, role, StringComparison.OrdinalIgnoreCase) ||
                                      userRole.EndsWith("\\" + role, StringComparison.OrdinalIgnoreCase)));

        if (!hasRequiredRole)
        {
            context.Result = new ForbidObjectResult(new ApiErrorResponse
            {
                Code = "admin.insufficient_permissions",
                NumericCode = ErrorCodes.AuthForbidden,
                Message = $"Доступ запрещён. Требуется одна из ролей: {string.Join(", ", rolesToCheck)}"
            });
            return;
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Результат 403 Forbidden с JSON телом.
/// </summary>
public sealed class ForbidObjectResult : ObjectResult
{
    public ForbidObjectResult(object value) : base(value)
    {
        StatusCode = 403;
    }
}
