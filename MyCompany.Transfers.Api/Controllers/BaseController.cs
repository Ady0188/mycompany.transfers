using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Api.Helpers;

namespace MyCompany.Transfers.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected IActionResult Problem(List<Error> errors)
    {
        if (errors.Count == 0) return Problem();
        if (errors.All(e => e.Type == ErrorType.Validation))
        {
            var apiErrors = errors.Select(e => e.ToApiErrorResponse(HttpContext)).ToList();
            return StatusCode(StatusCodes.Status400BadRequest, apiErrors);
        }
        return Problem(errors[0]);
    }

    protected IActionResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
        var apiError = error.ToApiErrorResponse(HttpContext);
        return StatusCode(statusCode, apiError);
    }
}
