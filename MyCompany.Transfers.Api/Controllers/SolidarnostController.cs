using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyCompany.Transfers.Application.Transfers.Commands;
using MyCompany.Transfers.Application.Transfers.Queries;
using MyCompany.Transfers.Contract;
using MyCompany.Transfers.Contract.Solidarnost;
using MyCompany.Transfers.Contract.Solidarnost.Requests;
using System.Net;
using ContractTransferMethod = MyCompany.Transfers.Contract.Core.Requests.TransferMethod;
using DomainTransferMethod = MyCompany.Transfers.Domain.Transfers.TransferMethod;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Протокол Solidarnost (payment_app): GET с query-параметрами, ответ XML.
/// Маппинг: nmtcheck/clientcheck → Check, payment → Prepare, paycheck → GetStatus.
/// </summary>
[ApiController]
[Route(ApiEndpoints.Solidarnost.Base)]
[Produces("application/xml")]
public sealed class SolidarnostController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly SolidarnostOptions _options;

    public SolidarnostController(ISender mediator, IOptions<SolidarnostOptions> options)
    {
        _mediator = mediator;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<IActionResult> ProcessAsync([FromQuery] TransferRequest request, CancellationToken ct)
    {
        try
        {
            string xml;
            switch (request.Action?.ToLowerInvariant())
            {
                case "nmtcheck":
                case "clientcheck":
                    xml = await HandleCheckAsync(request, ct);
                    break;
                case "payment":
                    xml = await HandlePaymentAsync(request, ct);
                    break;
                case "paycheck":
                    xml = await HandlePaycheckAsync(request, ct);
                    break;
                default:
                    xml = BuildErrorXml(300, "Invalid request");
                    break;
            }
            return CreateHttpResponse(xml);
        }
        catch (Exception)
        {
            return CreateHttpResponse(BuildErrorXml(300, "Internal error"));
        }
    }

    private async Task<string> HandleCheckAsync(TransferRequest r, CancellationToken ct)
    {
        var method = (r.Account?.Length == 16) ? DomainTransferMethod.ByPan : DomainTransferMethod.ByPhone;
        string serviceId = method == DomainTransferMethod.ByPan ? _options.PanServiceId : _options.PhoneServiceId;

        var cmd = new CheckCommand(
            _options.AgentId,
            serviceId,
            method,
            r.Account ?? string.Empty);

        var result = await _mediator.Send(cmd, ct);
        if (result.IsError)
            return BuildErrorXml(300, result.FirstError.Description);

        return @"<response><CODE>0</CODE><MESSAGE>OK</MESSAGE></response>";
    }

    private async Task<string> HandlePaymentAsync(TransferRequest r, CancellationToken ct)
    {
        var amountMinor = (long)Math.Round((r.Amount ?? 0m) * 100m, MidpointRounding.AwayFromZero);
        var method = (r.Account?.Length == 16) ? ContractTransferMethod.ByPan : ContractTransferMethod.ByPhone;
        var externalId = r.Pay_Id ?? r.ExtId ?? Guid.NewGuid().ToString();

        var cmd = new PrepareCommand(
            _options.AgentId,
            _options.TerminalId,
            externalId,
            method,
            r.Account ?? string.Empty,
            amountMinor,
            r.Currency ?? _options.DefaultCurrency,
            r.Settlement_Curr,
            _options.ServiceId,
            new Dictionary<string, string>());

        var result = await _mediator.Send(cmd, ct);
        if (result.IsError)
            return BuildErrorXml(300, result.FirstError.Description);

        var dto = result.Value;
        var regDate = DateTime.UtcNow.ToString("dd.MM.yyyy_HH:mm:ss");
        return $@"<response><CODE>0</CODE><MESSAGE>Payment Successful</MESSAGE><EXT_ID>{EscapeXml(dto.ExternalId ?? externalId)}</EXT_ID><REG_DATE>{regDate}</REG_DATE></response>";
    }

    private async Task<string> HandlePaycheckAsync(TransferRequest r, CancellationToken ct)
    {
        var q = new GetStatusQuery(
            _options.AgentId,
            r.Pay_Id ?? r.ExtId,
            null);
        var result = await _mediator.Send(q, ct);
        if (result.IsError)
            return BuildErrorXml(300, result.FirstError.Description);

        var dto = result.Value;
        return $@"<response><CODE>0</CODE><STATUS>{EscapeXml(dto.Status)}</STATUS><MESSAGE>{EscapeXml(dto.StatusMessage ?? dto.Status)}</MESSAGE></response>";
    }

    private static IActionResult CreateHttpResponse(string content)
    {
        var xmlResponse = "<?xml version=\"1.0\" encoding=\"windows-1251\"?>\n" + content.Trim();
        return new ContentResult
        {
            Content = xmlResponse,
            ContentType = "application/xml",
            StatusCode = (int)HttpStatusCode.OK
        };
    }

    private static string BuildErrorXml(int code, string message)
    {
        return $@"<response><CODE>{code}</CODE><MESSAGE>{EscapeXml(message)}</MESSAGE></response>";
    }

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
