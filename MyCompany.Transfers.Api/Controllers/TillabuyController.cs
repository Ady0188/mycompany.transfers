using ErrorOr;
using MyCompany.Transfers.Api.Helpers;
using MyCompany.Transfers.Application.Agents.Queries;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.MyCompanyTransfers.Commands;
using MyCompany.Transfers.Application.MyCompanyTransfers.Queries;
using MyCompany.Transfers.Application.Services.Queries;
using MyCompany.Transfers.Contract.Tillabuy.Requests;
using MyCompany.Transfers.Contract.Tillabuy.Responses;
using MyCompany.Transfers.Domain.Services;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NLog;
using System.Text.Json;

namespace MyCompany.Transfers.Api.Controllers;

[ApiController]
[Route("api")]
[Produces("application/xml")]
[Consumes("application/xml")]
[UseCustomXml]
[ApiExplorerSettings(GroupName = "tillabuy")]
public class TillabuyController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private delegate Task<IActionResult> QueriesDelegate(HttpRequest request);
    private readonly Dictionary<string, QueriesDelegate> Queries;

    private readonly Dictionary<string, string> agents;
    private readonly Dictionary<string, string> terms;
    private readonly Dictionary<string, string> termsCurrency;
    private readonly Dictionary<int, string> paramsDict;

    public TillabuyController(ISender mediator, IConfiguration configuration, IConfiguration config)
    {
        _mediator = mediator;

        Queries = new Dictionary<string, QueriesDelegate>
            {
                { "payment", PaymentAsync },
                { "check", CheckAsync },
                //{ "prepare", PrepareAsync },
                //{ "getproducts", GetProductsAsync },
                { "getbalance", GetBalanceAsync },
                { "getrate", GetRateAsync }
            };

        agents = config.GetSection("Agents").Get<Dictionary<string, string>>()!;
        terms = config.GetSection("Terminals").Get<Dictionary<string, string>>()!;
        termsCurrency = config.GetSection("TermsCurrency").Get<Dictionary<string, string>>()!;

        paramsDict = new Dictionary<int, string>
        {
            { 1, "account" },
            { 901, "sender_fullname" },
            { 902, "sender_doc_type" },
            { 903, "sender_doc_number" },
            { 904, "sender_phone" },
            { 905, "sender_doc_issuer" },
            { 906, "sender_doc_issue_date" },
            { 907, "sender_birth_date" },
            { 908, "sender_birth_place" },
            { 909, "sender_citizenship" },
            { 910, "sender_registration_address" },
            { 911, "sender_doc_department_code" },
            { 920, "sender_lastname" },
            { 921, "sender_firstname" },
            { 922, "sender_middlename" },
            { 932, "sender_residency" },
            { 934, "receiver_firstname" },
            { 936, "account_number" }
        };
    }

    private async Task<IActionResult> GetRateAsync(HttpRequest request)
    {
        var queryParams = request.Query.GetParameters();

        var clientGetRateQuery = new GetFxRateQuery("RUB", queryParams["curisocode"]);

        var clientGetRateResult = await _mediator.Send(clientGetRateQuery);

        if (clientGetRateResult.IsError)
            return Ok(new BaseResponse
            {
                Result = "Error",
                ErrCode = -1,
                Description = string.Join(" ", clientGetRateResult.Errors.Select(e => e.Description))
            });

        return Ok(new GetRateResponse
        {
            Result = "OK",
            ErrCode = 0,
            Rate = clientGetRateResult.Value.Rate
        });
    }

    private async Task<IActionResult> GetBalanceAsync(HttpRequest request)
    {
        var queryParams = request.Query.GetParameters();
        var term = terms[queryParams["termid"]];
        var agent = agents[term];

        var clientGetBalanceQuery = new GetBalanceQuery(agent, queryParams["curisocode"]);

        var clientGetBalanceResult = await _mediator.Send(clientGetBalanceQuery);

        if (clientGetBalanceResult.IsError)
            return Ok(new BaseResponse
            {
                Result = "Error",
                ErrCode = -1,
                Description = string.Join(" ", clientGetBalanceResult.Errors.Select(e => e.Description))
            });

        return Ok(new GetBalanceResponse
        {
            Result = "OK",
            ErrCode = 0,
            Balance = clientGetBalanceResult.Value.Balances.First().Amount
        });
    }

    private async Task<(bool IsSuccess, TillabuyTrn? Trn, Service? Service, BaseResponse? response)> CheckRequestAsync(HttpRequest request)
    {
        var queryParams = request.Query.GetParameters();

        var transaction = request.Query.MapToTransaction(agents, terms, termsCurrency, paramsDict);

        var agentQuery = new GetAgentByIdQuery(transaction.Agent);

        var agentResult = await _mediator.Send(agentQuery);

        if (agentResult.IsError)
        {
            return (false, null, null, new BaseResponse
            {
                Result = "Error",
                ErrCode = 8,
                Description = TillabuyExtensions.Errors[8]
            });
        }

        var agent = agentResult.Value;

        var serviceQuery = new GetServiceByIdQuery(transaction.Service);

        var serviceResult = await _mediator.Send(serviceQuery);

        if (serviceResult.IsError)
        {
            return (false, null, null, new BaseResponse
            {
                Result = "Error",
                ErrCode = 5,
                Description = TillabuyExtensions.Errors[5]
            });
        }

        var service = serviceResult.Value.Service;
        var isByPan = serviceResult.Value.IsByPan;

        if (isByPan)
        {
            var agentSettings = agent.SettingsJson.Deserialize<Dictionary<string, string>>();

            if (!agentSettings!.TryGetValue("PrivateKeyPath", out string? privateKeyPath) || string.IsNullOrEmpty(privateKeyPath))
            {
                return (false, null, null, new BaseResponse
                {
                    Result = "Error",
                    ErrCode = 14,
                    Description = TillabuyExtensions.Errors[14]
                });
            }

            agentSettings.TryGetValue("PrivateKeyPass", out string? privateKeyPass);

            var decrypted = transaction.Account.DecryptParameter(privateKeyPath, privateKeyPass);

            if (decrypted.IsError)
            {
                return (false, null, null, new BaseResponse
                {
                    Result = "Error",
                    ErrCode = 14,
                    Description = decrypted.Errors.First().Description
                });
            }

            transaction.Account = decrypted.Value;
            transaction.Account = "4279380036590729";
        }

        return (true, transaction, service, null);
    }

    //private async Task<IActionResult> GetProductsAsync(HttpRequest request)
    //{
    //    var getProductsQuery = new GetProductsQuery(request.QueryString.Value!);

    //    var getProductsResult = await _mediator.Send(getProductsQuery);

    //    if (getProductsResult.IsError)
    //        return Ok(new BaseResponse
    //        {
    //            Result = "Error",
    //            ErrCode = -1,
    //            Description = string.Join(" ", getProductsResult.Errors.Select(e => e.Description))
    //        });

    //    return Ok(getProductsResult.Value);
    //}

    //private async Task<IActionResult> PrepareAsync(HttpRequest request)
    //{
    //    string? extTermId = request.Query.GetTerminalId();

    //    var prepareQuery = new PrepareQuery(extTermId, request.QueryString.Value!);

    //    var prepareResult = await _mediator.Send(prepareQuery);

    //    if (prepareResult.IsError)
    //        return Ok(new BaseResponse
    //        {
    //            Result = "Error",
    //            ErrCode = int.Parse(prepareResult.Errors.First().Code),
    //            Description = string.Join(" ", prepareResult.Errors.Select(e => e.Description))
    //        });

    //    return Ok(prepareResult.Value);
    //}

    private async Task<IActionResult> CheckAsync(HttpRequest request)
    {
        var check = await CheckRequestAsync(request);

        if (!check.IsSuccess)
        {
            return Ok(check.response!);
        }

        var transaction = check.Trn!;

        //var queryParams = request.Query.GetParameters();

        //var transaction = request.Query.MapToTransaction(agents, terms, termsCurrency, paramsDict);

        //var agentQuery = new GetAgentByIdQuery(transaction.Agent);

        //var agentResult = await _mediator.Send(agentQuery);

        //if (agentResult.IsError)
        //{
        //    return Ok(new BaseResponse
        //    {
        //        Result = "Error",
        //        ErrCode = 8,
        //        Description = TillabuyExtensions.Errors[8]
        //    });
        //}

        //var agent = agentResult.Value;

        //var serviceQuery = new GetServiceByIdQuery(transaction.Service);

        //var serviceResult = await _mediator.Send(serviceQuery);

        //if (serviceResult.IsError)
        //{
        //    return Ok(new BaseResponse
        //    {
        //        Result = "Error",
        //        ErrCode = 5,
        //        Description = TillabuyExtensions.Errors[5]
        //    });
        //}

        //var service = serviceResult.Value.Service;
        //var isByPan = serviceResult.Value.IsByPan;

        //if (isByPan)
        //{
        //    var agentSettings = agent.SettingsJson.Deserialize<Dictionary<string, string>>();

        //    if (!agentSettings!.TryGetValue("PrivateKeyPath", out string? privateKeyPath) || string.IsNullOrEmpty(privateKeyPath))
        //    {
        //        return Ok(new BaseResponse
        //        {
        //            Result = "Error",
        //            ErrCode = 14,
        //            Description = TillabuyExtensions.Errors[14]
        //        });
        //    }

        //    agentSettings.TryGetValue("PrivateKeyPass", out string? privateKeyPass);

        //    var decrypted = transaction.Account.DecryptParameter(privateKeyPath, privateKeyPass);

        //    if (decrypted.IsError)
        //    {
        //        return Ok(new BaseResponse
        //        {
        //            Result = "Error",
        //            ErrCode = 14,
        //            Description = decrypted.Errors.First().Description
        //        });
        //    }

        //    transaction.Account = decrypted.Value;
        //}

        var prepareCommand = new PrepareCommand(transaction.Agent, transaction.TerminalId, transaction.PaymentExtId, 0, transaction.Account, transaction.Amount, "RUB", transaction.Currency, transaction.Service, transaction.Parameters);

        var prepareResult = await _mediator.Send(prepareCommand);

        if (prepareResult.IsError)
        {
            if (!TillabuyExtensions.ApiErrors.TryGetValue(prepareResult.Errors.First().Code, out int err))
                err = 14;

            return Ok(new BaseResponse
            {
                Result = "Error",
                ErrCode = err,
                Description = TillabuyExtensions.Errors[err]
            });
        }

        var response = prepareResult.Value.MapToResponse();
        _logger.Debug($"{transaction.PaymentExtId} Tillabut Response: {JsonSerializer.Serialize(response)}");

        return Ok(response);
    }

    private async Task<IActionResult> PaymentAsync(HttpRequest request)
    {
        var check = await CheckRequestAsync(request);

        if (!check.IsSuccess)
        {
            return Ok(check.response!);
        }

        var transaction = check.Trn!;
        var service = check.Service!;

        var getStatusQuery = new GetTransferByExternalIdQuery(transaction.Agent, transaction.PaymentExtId);

        var getStatusResult = await _mediator.Send(getStatusQuery);

        if (getStatusResult.IsError)
        {
            if (!TillabuyExtensions.ApiErrors.TryGetValue(getStatusResult.Errors.First().Code, out int err))
                err = 14;

            return Ok(new BaseResponse
            {
                Result = "Error",
                ErrCode = err,
                Description = TillabuyExtensions.Errors[err]
            });
        }

        var trn = getStatusResult.Value;

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
        else if (transaction.Service != trn.ServiceId ||
            transaction.Account != trn.Account ||
            !trn.Parameters.TryGetValue("sender_fullname", out string senderName) ||
            string.IsNullOrEmpty(senderName) ||
            transaction.SenderName != senderName)
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
                var res = new NKOPaymentResponse
                {
                    Balance = 0,
                    Description = "Платеж исполнен",
                    Result = "OK",
                    ErrCode = 0,
                    PaymExtId = transaction.PaymentExtId
                };

                if (trn.CurrentQuote!.CreditedAmount.Currency.Equals("RUB"))
                {
                    res.ExchangeRate = trn.CurrentQuote.ExchangeRate;
                    res.Currency = trn.CurrentQuote.CreditedAmount.Currency;
                    res.CreditAmount = trn.CurrentQuote.CreditedAmount.Minor;
                }

                return Ok(res);
            case TransferStatus.TECHNICAL:
            case TransferStatus.FAILED:
            case TransferStatus.EXPIRED: 
                return Ok(new BaseResponse
                {
                    Result = "Error",
                    ErrCode = 14,
                    Description = TillabuyExtensions.Errors[14]
                });
            case TransferStatus.FRAUD: 
                return Ok(new BaseResponse
                {
                    Result = "Error",
                    ErrCode = 55,
                    Description = TillabuyExtensions.Errors[55]
                });
        }

        var confirmCommand = new ConfirmCommand(transaction.Agent, transaction.TerminalId, transaction.PaymentExtId, trn.CurrentQuote!.Id);

        var confirmResult = await _mediator.Send(confirmCommand);

        if (confirmResult.IsError)
        {
            if (!TillabuyExtensions.ApiErrors.TryGetValue(confirmResult.Errors.First().Code, out int err))
                err = 14;

            return Ok(new BaseResponse
            {
                Result = "Error",
                ErrCode = err,
                Description = TillabuyExtensions.Errors[err]
            });
        }

        var response = confirmResult.Value.MapToResponse(trn.ProvReceivedParams, trn.CurrentQuote.ExchangeRate);
        _logger.Debug($"{transaction.PaymentExtId} Tillabut Response: {JsonSerializer.Serialize(response)}");

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> TillabayRequests([FromQuery] string function)
    {
        try
        {
            _logger.Debug($"PaymentController request: function={function}; {Request.QueryString.Value}");

            function = function.ToLower();

            if (Queries.ContainsKey(function))
            {
                QueriesDelegate selectedMethod = Queries[function];
                return await selectedMethod(Request);
            }

            _logger.Debug($"PaymentController BadRequest: function not found");
            return BadRequest();

        }
        catch (KeyNotFoundException knfEx)
        {
            _logger.Debug($"KeyNotFoundException: {knfEx}");
            int code = 5;
            if (knfEx.Message.StartsWith("TermId"))
                code = 8;

            return Ok(new BaseResponse
            {
                Result = "Error",
                ErrCode = code,
                Description = TillabuyExtensions.Errors[code]
            });
        }
        catch (Exception ex)
        {
            _logger.Debug($"Error: {ex}");
            return StatusCode(500);
        }
    }
}
