using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Contract.Core.Responses;

namespace MyCompany.Transfers.Api.Auth;

/// <summary>
/// Авторизация для административных эндпоинтов через статический API Key из конфигурации.
/// Ожидает заголовок X-Admin-Key со значением из конфига Admin:ApiKey.
/// </summary>
public sealed class AdminApiKeyAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var req = context.HttpContext.Request;
        var providedKey = req.Headers["X-Admin-Key"].ToString();

        var expectedKey = config["Admin:ApiKey"];

        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
            {
                Code = "admin.config_missing",
                NumericCode = 500,
                Message = "Административный API Key не настроен на сервере."
            });
            return;
        }

        if (string.IsNullOrWhiteSpace(providedKey) || !string.Equals(providedKey, expectedKey, StringComparison.Ordinal))
        {
            var unauthorizedError = AppErrors.Common.Unauthorized();
            context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
            {
                Code = unauthorizedError.Code,
                NumericCode = ErrorCodes.AuthUnauthorized,
                Message = "Неверный или отсутствующий административный API Key."
            });
            return;
        }

        await Task.CompletedTask;
    }
}
