using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MyCompany.Transfers.Api.Auth;

//public sealed class SignatureAuthHandler
//    : AuthenticationHandler<AuthenticationSchemeOptions>
//{
//    public SignatureAuthHandler(
//        IOptionsMonitor<AuthenticationSchemeOptions> opt,
//        ILoggerFactory logger,
//        UrlEncoder encoder,
//        ISystemClock clock) : base(opt, logger, encoder, clock) { }

//    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
//    {
//        // 1) Читаем заголовки
//        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
//            !Request.Headers.TryGetValue("X-Signature", out var sign))
//            return Task.FromResult(AuthenticateResult.NoResult());

//        if (string.IsNullOrWhiteSpace(apiKey) || /* string.IsNullOrWhiteSpace(ts) ||
//            string.IsNullOrWhiteSpace(nonce) ||*/ string.IsNullOrWhiteSpace(sign))
//        {
//            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return;
//        }

//        // 2) Валидация сигнатуры (ваша логика)
//        if (!SignatureValidator.IsValid(Request, apiKey!, sig!))
//            return Task.FromResult(AuthenticateResult.Fail("Invalid signature"));

//        // 3) Строим principal с нужными claim’ами
//        var claims = new[]
//        {
//            new Claim("agent_id", /*...*/ "agent_001"),
//            new Claim("terminal_id", /*...*/ "term_001")
//        };
//        var identity = new ClaimsIdentity(claims, Scheme.Name);
//        var principal = new ClaimsPrincipal(identity);
//        var ticket = new AuthenticationTicket(principal, Scheme.Name);
//        return Task.FromResult(AuthenticateResult.Success(ticket));
//    }
//}