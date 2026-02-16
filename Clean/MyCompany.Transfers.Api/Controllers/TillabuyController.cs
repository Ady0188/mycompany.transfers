using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MyCompany.Transfers.Application.Agents.Queries;
using MyCompany.Transfers.Application.Rates.Queries;
using MyCompany.Transfers.Application.Services.Queries;
using MyCompany.Transfers.Application.Transfers.Commands;
using MyCompany.Transfers.Application.Transfers.Queries;
using MyCompany.Transfers.Contract;
using MyCompany.Transfers.Contract.Tillabuy.Requests;
using MyCompany.Transfers.Contract.Tillabuy.Responses;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Api.Helpers;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// Протокол Tillabuy: XML, авторизация по конфигу (termid → agent).
/// Функции: check (prepare), payment (confirm/status), getbalance, getrate.
/// Коды ошибок по протоколу НКО — см. TillabuyExtensions.Errors / ApiErrors.
/// </summary>
[ApiController]
[Route(ApiEndpoints.TillabuyBase)]
[Consumes("application/xml", "application/json")]
[Produces("application/xml")]
[UseCustomXml]
[ApiExplorerSettings(GroupName = "tillabuy")]
public sealed class TillabuyController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TillabuyController> _logger;

    private readonly Dictionary<string, string> _agents;
    private readonly Dictionary<string, string> _terminals;
    private readonly Dictionary<string, string> _termsCurrency;

    public TillabuyController(ISender mediator, IConfiguration configuration, ILogger<TillabuyController> logger)
    {
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
        _agents = _configuration.GetSection("Tillabuy:Agents").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
        _terminals = _configuration.GetSection("Tillabuy:Terminals").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
        _termsCurrency = _configuration.GetSection("Tillabuy:TermsCurrency").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
    }

    [HttpGet]
    public async Task<IActionResult> Handle([FromQuery] string function, CancellationToken ct)
    {
        var functionLower = function?.ToLowerInvariant() ?? string.Empty;
        _logger.LogDebug("Tillabuy request: function={Function}, query={Query}", function, Request.QueryString);

        try
        {
            return functionLower switch
            {
                "check" => await CheckAsync(ct),
                "payment" => await PaymentAsync(ct),
                "getrate" => await GetRateAsync(ct),
                "getbalance" => await GetBalanceAsync(ct),
                _ => Ok(new BaseResponse { Result = "Error", ErrCode = -1, Description = "Unknown function" })
            };
        }
        catch (KeyNotFoundException knfEx)
        {
            _logger.LogDebug("Tillabuy KeyNotFoundException: {Message}", knfEx.Message);
            var code = knfEx.Message.StartsWith("TermId", StringComparison.OrdinalIgnoreCase) ? 8 : 5;
            var desc = TillabuyExtensions.Errors.TryGetValue(code, out var d) ? d : knfEx.Message;
            return Ok(new BaseResponse { Result = "Error", ErrCode = code, Description = desc });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tillabuy error");
            return StatusCode(500, new BaseResponse { Result = "Error", ErrCode = 9, Description = TillabuyExtensions.Errors[9] });
        }
    }

    private async Task<IActionResult> CheckAsync(CancellationToken ct)
    {
        var (success, transaction, service, errorResponse) = await CheckRequestAsync(ct);
        if (!success)
            return Ok(errorResponse!);

        var cmd = new PrepareCommand(
            transaction!.Agent,
            transaction.TerminalId,
            transaction.PaymentExtId,
            Contract.Core.Requests.TransferMethod.ByPhone,
            transaction.Account,
            transaction.Amount,
            "RUB",
            transaction.PayoutCurrency,
            transaction.Service,
            transaction.Parameters);

        var prepareResult = await _mediator.Send(cmd, ct);
        if (prepareResult.IsError)
        {
            var errCode = prepareResult.Errors.Count > 0 ? prepareResult.Errors[0].ToTillabuyErrorCode() : 14;
            var desc = TillabuyExtensions.Errors.TryGetValue(errCode, out var d) ? d : string.Join(" ", prepareResult.Errors.Select(e => e.Description));
            return Ok(new BaseResponse { Result = "Error", ErrCode = errCode, Description = desc });
        }

        var response = prepareResult.Value.MapToResponse();
        _logger.LogDebug("Tillabuy check PaymExtId={PaymExtId}", transaction.PaymentExtId);
        return Ok(response);
    }

    private async Task<IActionResult> PaymentAsync(CancellationToken ct)
    {
        var (success, transaction, service, errorResponse) = await CheckRequestAsync(ct);
        if (!success)
            return Ok(errorResponse!);

        var statusResult = await _mediator.Send(new GetTransferByExternalIdQuery(transaction!.Agent, transaction.PaymentExtId), ct);
        if (statusResult.IsError)
        {
            var errCode = statusResult.Errors.Count > 0 ? statusResult.Errors[0].ToTillabuyErrorCode() : 14;
            var desc = TillabuyExtensions.Errors.TryGetValue(errCode, out var d) ? d : string.Join(" ", statusResult.Errors.Select(e => e.Description));
            return Ok(new BaseResponse { Result = "Error", ErrCode = errCode, Description = desc });
        }

        var trn = statusResult.Value;

        if (trn.Amount.Minor != transaction.Amount)
        {
            return Ok(new NKOPaymentResponse
            {
                Balance = 0,
                Description = TillabuyExtensions.Errors[41],
                Result = "Error",
                ErrCode = 41,
                PaymExtId = transaction.PaymentExtId
            });
        }

        if (transaction.Service != trn.ServiceId || transaction.Account != trn.Account)
        {
            return Ok(new NKOPaymentResponse
            {
                Balance = 0,
                Description = TillabuyExtensions.Errors[42],
                Result = "Error",
                ErrCode = 42,
                PaymExtId = transaction.PaymentExtId
            });
        }

        if (!trn.Parameters.TryGetValue("sender_fullname", out var senderName) || string.IsNullOrEmpty(senderName) || transaction.SenderName != senderName)
        {
            return Ok(new NKOPaymentResponse
            {
                Balance = 0,
                Description = TillabuyExtensions.Errors[42],
                Result = "Error",
                ErrCode = 42,
                PaymExtId = transaction.PaymentExtId
            });
        }

        switch (trn.Status)
        {
            case TransferStatus.CONFIRMED:
                return Ok(new NKOPaymentResponse
                {
                    Balance = 0,
                    Description = TillabuyExtensions.Errors[15],
                    Result = "OK",
                    ErrCode = 15,
                    PaymExtId = transaction.PaymentExtId
                });
            case TransferStatus.SUCCESS:
                var successResponse = new NKOPaymentResponse
                {
                    Balance = 0,
                    Description = "Платеж исполнен",
                    Result = "OK",
                    ErrCode = 0,
                    PaymExtId = transaction.PaymentExtId
                };
                if (trn.CurrentQuote is not null && !string.Equals(trn.CurrentQuote.CreditedAmount.Currency, "RUB", StringComparison.OrdinalIgnoreCase))
                {
                    successResponse.ExchangeRate = trn.CurrentQuote.ExchangeRate;
                    successResponse.Currency = trn.CurrentQuote.CreditedAmount.Currency;
                    successResponse.CreditAmount = trn.CurrentQuote.CreditedAmount.Minor;
                }
                return Ok(successResponse);
            case TransferStatus.TECHNICAL:
            case TransferStatus.FAILED:
            case TransferStatus.EXPIRED:
                return Ok(new BaseResponse { Result = "Error", ErrCode = 14, Description = TillabuyExtensions.Errors[14] });
            case TransferStatus.FRAUD:
                return Ok(new BaseResponse { Result = "Error", ErrCode = 55, Description = TillabuyExtensions.Errors[55] });
        }

        var confirmCmd = new ConfirmCommand(transaction.Agent, transaction.TerminalId, transaction.PaymentExtId, trn.CurrentQuote!.Id);
        var confirmResult = await _mediator.Send(confirmCmd, ct);
        if (confirmResult.IsError)
        {
            var errCode = confirmResult.Errors.Count > 0 ? confirmResult.Errors[0].ToTillabuyErrorCode() : 14;
            var desc = TillabuyExtensions.Errors.TryGetValue(errCode, out var d) ? d : string.Join(" ", confirmResult.Errors.Select(e => e.Description));
            return Ok(new BaseResponse { Result = "Error", ErrCode = errCode, Description = desc });
        }

        var response = confirmResult.Value.MapToResponse(
            trn.ProvReceivedParams,
            trn.CurrentQuote?.ExchangeRate,
            trn.CurrentQuote != null ? new Domain.Transfers.Dtos.MoneyDto { Currency = trn.CurrentQuote.CreditedAmount.Currency, Amount = trn.CurrentQuote.CreditedAmount.Minor } : null);
        _logger.LogDebug("Tillabuy payment PaymExtId={PaymExtId}", transaction.PaymentExtId);
        return Ok(response);
    }

    private async Task<IActionResult> GetRateAsync(CancellationToken ct)
    {
        var queryParams = Request.Query.GetParameters();
        if (!TryResolveAgent(queryParams, out var agentId, out var error))
            return Ok(error!);

        var baseCurrency = queryParams.GetValueOrDefault("basecurrency", "RUB");
        var quoteCurrency = queryParams.GetValueOrDefault("curisocode", "TJS");

        var result = await _mediator.Send(new GetFxRateQuery(agentId!, baseCurrency, quoteCurrency), ct);
        if (result.IsError)
            return Ok(new BaseResponse { Result = "Error", ErrCode = -1, Description = string.Join(" ", result.Errors.Select(e => e.Description)) });

        return Ok(new GetRateResponse { Result = "OK", ErrCode = 0, Rate = result.Value.Rate });
    }

    private async Task<IActionResult> GetBalanceAsync(CancellationToken ct)
    {
        var queryParams = Request.Query.GetParameters();
        if (!TryResolveAgent(queryParams, out var agentId, out var error))
            return Ok(error!);

        var currency = queryParams.GetValueOrDefault("curisocode", "TJS");
        var result = await _mediator.Send(new GetBalanceQuery(agentId!, currency), ct);
        if (result.IsError)
            return Ok(new BaseResponse { Result = "Error", ErrCode = -1, Description = string.Join(" ", result.Errors.Select(e => e.Description)) });

        var balance = result.Value.Balances.FirstOrDefault(b => string.Equals(b.Currency, currency, StringComparison.OrdinalIgnoreCase));
        return Ok(new GetBalanceResponse { Result = "OK", ErrCode = 0, Balance = (balance?.Amount ?? 0) / 100m });
    }

    private async Task<(bool IsSuccess, TillabuyTrn? Trn, Domain.Services.Service? Service, BaseResponse? ErrorResponse)> CheckRequestAsync(CancellationToken ct)
    {
        var queryParams = Request.Query;
        TillabuyTrn transaction;
        try
        {
            transaction = queryParams.MapToTransaction(_agents, _terminals, _termsCurrency);
        }
        catch (KeyNotFoundException)
        {
            throw;
        }

        var agentResult = await _mediator.Send(new GetAgentByIdQuery(transaction.Agent), ct);
        if (agentResult.IsError)
        {
            var errCode = agentResult.Errors[0].ToTillabuyErrorCode();
            return (false, null, null, new BaseResponse { Result = "Error", ErrCode = errCode, Description = TillabuyExtensions.Errors.GetValueOrDefault(errCode, agentResult.Errors[0].Description) });
        }

        var agent = agentResult.Value;

        var serviceResult = await _mediator.Send(new GetServiceByIdQuery(transaction.Service), ct);
        if (serviceResult.IsError)
        {
            var errCode = 5;
            return (false, null, null, new BaseResponse { Result = "Error", ErrCode = errCode, Description = TillabuyExtensions.Errors.GetValueOrDefault(errCode, "Service not found") });
        }

        var (service, isByPan) = serviceResult.Value;

        if (isByPan)
        {
            var agentSettings = JsonSerializer.Deserialize<Dictionary<string, string>>(agent.SettingsJson) ?? new Dictionary<string, string>();
            if (!agentSettings.TryGetValue("PrivateKeyPath", out var privateKeyPath) || string.IsNullOrWhiteSpace(privateKeyPath))
            {
                return (false, null, null, new BaseResponse
                {
                    Result = "Error",
                    ErrCode = 14,
                    Description = TillabuyExtensions.Errors.GetValueOrDefault(14, "PrivateKeyPath not configured")
                });
            }
            agentSettings.TryGetValue("PrivateKeyPass", out var privateKeyPass);

            var decrypted = transaction.Account.DecryptParameter(privateKeyPath, privateKeyPass);
            if (decrypted.IsError)
            {
                return (false, null, null, new BaseResponse
                {
                    Result = "Error",
                    ErrCode = 14,
                    Description = decrypted.Errors.Count > 0 ? decrypted.Errors[0].Description : TillabuyExtensions.Errors[14]
                });
            }
            transaction.Account = decrypted.Value;
        }

        return (true, transaction, service, null);
    }

    private bool TryResolveAgent(Dictionary<string, string> queryParams, out string? agentId, out BaseResponse? errorResponse)
    {
        agentId = null;
        errorResponse = null;
        if (!queryParams.TryGetValue("termid", out var termid) || string.IsNullOrWhiteSpace(termid))
        {
            errorResponse = new BaseResponse { Result = "Error", ErrCode = 8, Description = TillabuyExtensions.Errors.GetValueOrDefault(8, "TermId required") };
            return false;
        }
        if (!_terminals.TryGetValue(termid, out var termKey))
        {
            errorResponse = new BaseResponse { Result = "Error", ErrCode = 8, Description = TillabuyExtensions.Errors.GetValueOrDefault(8, "Terminal not found") };
            return false;
        }
        if (!_agents.TryGetValue(termKey, out var agent))
        {
            errorResponse = new BaseResponse { Result = "Error", ErrCode = 5, Description = TillabuyExtensions.Errors.GetValueOrDefault(5, "Agent not found") };
            return false;
        }
        agentId = agent;
        return true;
    }
}
