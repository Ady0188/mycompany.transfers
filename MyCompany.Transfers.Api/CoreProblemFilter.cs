using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyCompany.Transfers.Api;

public sealed class CoreProblemFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        try
        {
            var result = await next(ctx);
            return result;
        }
        catch (Exception)
        {
            return Results.Problem(
                title: "Unexpected error.",
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?>
                {
                    ["traceId"] = ctx.HttpContext.TraceIdentifier
                });
        }
    }
}

public sealed class CoreProblemExceptionFilter : IAsyncExceptionFilter
{
    public Task OnExceptionAsync(ExceptionContext context)
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = "Unexpected error.",
            Status = StatusCodes.Status500InternalServerError,
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        context.Result = new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" }
        };
        context.ExceptionHandled = true;
        return Task.CompletedTask;
    }
}