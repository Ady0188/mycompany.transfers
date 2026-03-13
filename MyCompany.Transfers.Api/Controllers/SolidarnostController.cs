using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyCompany.Transfers.Api.Helpers;
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
                    xml = await HandleNmtCheckAsync(request, ct);
                    break;
                case "clientcheck":
                    xml = await HandleClientCheckAsync(request, ct);
                    break;
                case "payment":
                    xml = await HandlePaymentAsync(request, ct);
                    break;
                case "paycheck":
                    xml = await HandlePaycheckAsync(request, ct);
                    break;
                default:
                    xml = SolidarnostErrorCodes.InvalidRequest;
                    break;
            }
            return CreateHttpResponse(xml);
        }
        catch (Exception)
        {
            return CreateHttpResponse(SolidarnostErrorCodes.InternalError);
        }
    }

    /// <summary>nmtcheck: ответ с CREDIT_AMOUNT (как в Solidarnost.Api NmtCheckAsync).</summary>
    private async Task<string> HandleNmtCheckAsync(TransferRequest r, CancellationToken ct)
    {
        var method = (r.Account?.Length == 16) ? DomainTransferMethod.ByPan : DomainTransferMethod.ByPhone;
        var serviceId = method == DomainTransferMethod.ByPan ? _options.PanServiceId : _options.PhoneServiceId;

        var cmd = new CheckCommand(_options.AgentId, serviceId, method, r.Account ?? string.Empty);
        var result = await _mediator.Send(cmd, ct);
        if (result.IsError)
            return SolidarnostErrorMapper.Map(result.FirstError, action: "nmtcheck");

        var p = result.Value.ResolvedParameters;
        var currency = result.Value.AvailableCurrencies.First().Currency;
        var amount = r.Amount / 100m * result.Value.AvailableCurrencies.First().Rate;

        string? Get(string key) => p.TryGetValue(key, out var v) ? v : null;

        var firstname = Get("receiver_firstname_cyr");
        var lastname = Get("receiver_lastname_cyr");
        var middlename = Get("receiver_middlename_cyr");
        var fio = $"{firstname} {middlename} {lastname?[0]}.".Trim().Replace("  ", " ");
        var creditCurr = result.Value.AvailableCurrencies.First().Currency;
        var receiverFee = Get("RECEIVER_FEE") ?? "0";
        var receiverExtId = Get("account_id");
        var receiverAccount = Get("account_number");
        var receiverCard = Get("RECEIVER_CARD");
        decimal creditAmount = r.Amount / 100m * result.Value.AvailableCurrencies.First().Rate ?? 0;
        decimal? currRate = result.Value.AvailableCurrencies.First().Rate;

        return SolidarnostResponseBuilder.NmtCheckSuccess(
            fio,
            creditAmount,
            creditCurr,
            currRate,
            receiverFee,
            receiverExtId,
            receiverAccount,
            receiverCard);
    }

    /// <summary>clientcheck: ответ без CREDIT_AMOUNT, опционально RECEIVER_ACCOUNT (как в Solidarnost.Api ClientCheckAsync).</summary>
    private async Task<string> HandleClientCheckAsync(TransferRequest r, CancellationToken ct)
    {
        var method = (r.Account?.Length == 16) ? DomainTransferMethod.ByPan : DomainTransferMethod.ByPhone;
        var serviceId = method == DomainTransferMethod.ByPan ? _options.PanServiceId : _options.PhoneServiceId;

        var cmd = new CheckCommand(_options.AgentId, serviceId, method, r.Account ?? string.Empty);
        var result = await _mediator.Send(cmd, ct);
        if (result.IsError)
            return SolidarnostErrorMapper.Map(result.FirstError, action: "clientcheck");

        return SolidarnostResponseBuilder.ClientCheckSuccess();
    }

    /// <summary>payment: ответ как MapToSuccessResponse в Solidarnost.Api (CODE 0, EXT_ID, REG_DATE).</summary>
    private async Task<string> HandlePaymentAsync(TransferRequest r, CancellationToken ct)
    {
        var amountMinor = (long)Math.Round((r.Amount ?? 0m), MidpointRounding.AwayFromZero);
        var method = (r.Account?.Length == 16) ? ContractTransferMethod.ByPan : ContractTransferMethod.ByPhone;
        var serviceId = (r.Account?.Length == 16) ? _options.PanServiceId : _options.PhoneServiceId;
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
            serviceId,
            new Dictionary<string, string>());

        var result = await _mediator.Send(cmd, ct);
        if (result.IsError)
            return SolidarnostErrorMapper.Map(result.FirstError, action: "payment");

        var dto = result.Value;
        var extId = dto.ExternalId ?? externalId;
        var regDate = DateTime.UtcNow.ToString("dd.MM.yyyy_HH:mm:ss");
        return SolidarnostResponseBuilder.PaymentSuccess(extId, regDate);
    }

    /// <summary>paycheck: при найденном платеже — тот же формат, что и payment (MapToSuccessResponse); иначе CODE 707.</summary>
    private async Task<string> HandlePaycheckAsync(TransferRequest r, CancellationToken ct)
    {
        var q = new GetStatusQuery(_options.AgentId, r.Pay_Id ?? r.ExtId, null);
        var result = await _mediator.Send(q, ct);
        if (result.IsError)
            return SolidarnostErrorMapper.Map(result.FirstError, action: "paycheck");

        var dto = result.Value;
        var extId = dto.ExternalId ?? r.Pay_Id ?? r.ExtId ?? string.Empty;
        var regDate = dto.ConfirmedAt?.Utc ?? dto.CompletedAt?.Utc ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var regDateFormatted = DateTime.TryParse(regDate, out var dt) ? dt.ToString("dd.MM.yyyy_HH:mm:ss") : DateTime.UtcNow.ToString("dd.MM.yyyy_HH:mm:ss");
        return SolidarnostResponseBuilder.PaymentSuccess(extId, regDateFormatted);
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
}
