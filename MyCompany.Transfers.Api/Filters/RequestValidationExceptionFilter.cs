using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyCompany.Transfers.Application.Common.Behaviors;
using MyCompany.Transfers.Api.Helpers;

namespace MyCompany.Transfers.Api.Filters;

/// <summary>
/// Преобразует <see cref="RequestValidationException"/> из pipeline валидации в 400 Bad Request
/// с телом в формате списка <see cref="Contract.Core.Responses.ApiErrorResponse"/>.
/// </summary>
public sealed class RequestValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not RequestValidationException ex)
            return;

        context.ExceptionHandled = true;
        var apiErrors = ex.Errors
            .Select(e => e.ToApiErrorResponse(context.HttpContext))
            .ToList();
        context.Result = new ObjectResult(apiErrors)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            DeclaredType = typeof(List<Contract.Core.Responses.ApiErrorResponse>)
        };
    }
}
