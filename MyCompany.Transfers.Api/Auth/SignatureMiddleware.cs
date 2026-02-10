using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Contract.Core.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static System.Net.WebRequestMethods;

namespace MyCompany.Transfers.Api.Auth;

public sealed class SignatureAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var services = context.HttpContext.RequestServices;
        var terminals = services.GetRequiredService<ITerminalRepository>();

        var req = context.HttpContext.Request;
        var apiKey = req.Headers["X-Api-Key"].ToString();
        var sign = req.Headers["X-MyCompany-Signature"].ToString();
        var unauthorizeError = AppErrors.Common.Unauthorized();

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(sign))
        { context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
        {
            Code = unauthorizeError.Code,
            NumericCode = ErrorCodes.AuthUnauthorized,
            Message = unauthorizeError.Description,
        }); return; }

        var term = await terminals.GetByApiKeyAsync(apiKey, context.HttpContext.RequestAborted);
        if (term is null) { context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
        {
            Code = unauthorizeError.Code,
            NumericCode = ErrorCodes.AuthUnauthorized,
            Message = unauthorizeError.Description,
        }); return; }

        req.EnableBuffering();
        using var sr = new StreamReader(req.Body, Encoding.UTF8, leaveOpen: true);
        var body = await sr.ReadToEndAsync();
        req.Body.Position = 0;

        using var sha = SHA256.Create();
        var bodyHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(body ?? "")));
        var expected = ComputeHmac(term.Secret, bodyHash);

        var signHex = req.Headers["X-MyCompany-Signature"].ToString();
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(signHex), Convert.FromHexString(expected)))
        {
            var sugnatureInvalid = AppErrors.Auth.SignatureInvalid;
            context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
            {
                Code = sugnatureInvalid.Code,
                NumericCode = ErrorCodes.AuthUnauthorized,
                Message = sugnatureInvalid.Description,
            }); return;
        }

        var id = new ClaimsIdentity(new[]
        {
            new Claim("agent_id", term.AgentId),
            new Claim("terminal_id", term.Id),
        }, "Sig");
        context.HttpContext.User = new ClaimsPrincipal(id);
    }

    static string ComputeHmac(string secret, string msg)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(msg)));
    }
}