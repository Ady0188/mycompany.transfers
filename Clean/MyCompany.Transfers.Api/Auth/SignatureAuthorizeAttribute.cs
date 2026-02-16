using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Contract.Core.Responses;

namespace MyCompany.Transfers.Api.Auth;

public sealed class SignatureAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var terminals = context.HttpContext.RequestServices.GetRequiredService<ITerminalRepository>();
        var req = context.HttpContext.Request;
        var apiKey = req.Headers["X-Api-Key"].ToString();
        var sign = req.Headers["X-MyCompany-Signature"].ToString();
        var unauthorizedError = AppErrors.Common.Unauthorized();

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(sign))
        {
            context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
            {
                Code = unauthorizedError.Code,
                NumericCode = ErrorCodes.AuthUnauthorized,
                Message = unauthorizedError.Description
            });
            return;
        }

        var term = await terminals.GetByApiKeyAsync(apiKey, context.HttpContext.RequestAborted);
        if (term is null)
        {
            context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
            {
                Code = AppErrors.Auth.TerminalNotFound(apiKey).Code,
                NumericCode = ErrorCodes.TerminalNotFound,
                Message = unauthorizedError.Description
            });
            return;
        }

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
            var signatureInvalid = AppErrors.Auth.SignatureInvalid;
            context.Result = new UnauthorizedObjectResult(new ApiErrorResponse
            {
                Code = signatureInvalid.Code,
                NumericCode = ErrorCodes.AuthBadSignature,
                Message = signatureInvalid.Description
            });
            return;
        }

        var identity = new ClaimsIdentity(new[]
        {
            new Claim("agent_id", term.AgentId),
            new Claim("terminal_id", term.Id)
        }, "Sig");
        context.HttpContext.User = new ClaimsPrincipal(identity);
    }

    private static string ComputeHmac(string secret, string msg)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(msg)));
    }
}
