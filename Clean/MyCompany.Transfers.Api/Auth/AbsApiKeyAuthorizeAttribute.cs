using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Contract.Core.Responses;

namespace MyCompany.Transfers.Api.Auth;

/// <summary>
/// Авторизация для эндпоинтов АБС (автоматизированная банковская система).
/// Ожидает заголовок X-Abs-Api-Key со значением из конфига Abs:ApiKey.
/// Используется для операций кредитования/дебитования баланса агента и получения курсов со стороны АБС.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AbsApiKeyAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var req = context.HttpContext.Request;
        var providedKey = req.Headers["X-Abs-Api-Key"].ToString();

        var expectedKey = config["Abs:ApiKey"];

        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
            {
                Code = "abs.config_missing",
                NumericCode = 500,
                Message = "API Key АБС не настроен на сервере."
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
                Message = "Неверный или отсутствующий API Key АБС (заголовок X-Abs-Api-Key)."
            });
            return;
        }

        await Task.CompletedTask;
    }
}
