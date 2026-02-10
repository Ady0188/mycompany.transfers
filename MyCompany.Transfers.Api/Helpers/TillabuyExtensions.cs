using ErrorOr;
using MyCompany.Transfers.Contract.Tillabuy.Requests;
using MyCompany.Transfers.Contract.Tillabuy.Responses;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Transfers.Dtos;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MyCompany.Transfers.Api.Helpers;

public static class TillabuyExtensions
{
    public static Dictionary<int, string> Errors = new Dictionary<int, string>(){
        {0, "OK" },
        {2, "Пункт приема платежа не зарегистрирован или блокирован, либо в запросе указано некорректное значение TermType." },
        {4, "Отсутствует параметр PaymExtId или не задано его значение; при отправке запроса использован метод, отличный от GET." },
        {5, "Код назначения платежа, указанный в параметре PaymSubjTp, не зарегистрирован в Системе." },
        {8, "Значения параметров Params, Amount, TermType, PaymExtId, TermId, TermTime не соответствуют установленным шаблонам или требованиям для данного кода назначения." },
        {9, "Временная проблема с обработкой запроса на стороне биллинга МБТ." },
        {10, "Сумма платежа, переданная в параметре Amount, не соответствует установленным для данного кода назначения ограничениям." },
        {14, "Платеж по данной операции уже создан, находится в состоянии Не подготовлен." },
        {41, "Сумма платежа, переданная в параметре Amount, отличается от аналогичной суммы, переданной в исходном запросе." },
        {42, "Значение, переданное в параметрах PaymSubjTp, Params или TermType, отличается от аналогичных значений, переданных в исходном запросе." },
        {30, "Недостаточно денежных средств на балансе Банка в МБТ" },
        {141, "Отказ ИС Получателя в приеме платежа." },
        {142, "Система банка партнера временно не доступна, повторите попытку чуть позже." },
        {143, "Платеж по данной операции не найден." },
        {144, "Время на подверждение перевода истекло" },
        {55, "Нарушены правила финансового или fraud-мониторинга." },
        {15, "Не получен ответ от ИС Получателя за установленный регламентом интервал времени." }
    };

    public static Dictionary<string, int> ApiErrors = new Dictionary<string, int>(){
        { "OK", 0 },
        { "common.unexpected",  9},
        { "common.validation",  8},
        { "auth.unauthorized",  2},
        { "auth.forbidden", 55},
        { "common.not_found",   143},
        { "common.invalid_request", 4},
        { "transfer.not_found", 143},
        { "transfer.not_prepared",  14},
        { "transfer.already_finished",  14},
        { "transfer.external_id_conflict",  14},
        { "transfer.already_confirmed", 14},
        { "transfer.quote_mismatch",    42},
        { "transfer.quote_expired", 144},
        { "transfer.invalid_request",   8},
        { "agent.not_found",    2},
        { "agent.insufficient_balance", 30},
        { "auth.bad_signature", 2},
        { "auth.signature_expired", 2},
        { "auth.terminal_not_found",    2}
    };

    public static ErrorOr<string> DecryptParameter(this string input, string privateKeyPath, string? privateKeyPass = null)
    {
        try
        {
            if (privateKeyPath.EndsWith(".key"))
            {
                string privateKey = File.ReadAllText(privateKeyPath);

                // Create an RSA object
                using (RSA rsa = RSA.Create())
                {
                    // Import the private key from PEM format
                    rsa.ImportFromEncryptedPem(privateKey, privateKeyPass);
                    //rsa.ImportFromPem(privateKey);

                    // Decode the Base64 encrypted data to byte array
                    byte[] encryptedData = Convert.FromBase64String(Uri.UnescapeDataString(input));

                    // Decrypt data
                    byte[] decryptedData = rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA1);

                    // Convert decrypted byte array back to string (assuming the original data was encoded in windows-1251)
                    return Encoding.GetEncoding("windows-1251").GetString(decryptedData);
                }
            }
            else
            {
                // Load the private key
                X509Certificate2 cert = new X509Certificate2(privateKeyPath, privateKeyPass, X509KeyStorageFlags.Exportable);

                // Get RSA private key from the certificate
                RSA rsa = cert.GetRSAPrivateKey();

                // Decode the Base64 encrypted data to byte array
                byte[] encryptedData = Convert.FromBase64String(Uri.UnescapeDataString(input));

                // Decrypt data
                byte[] decryptedData = rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA1);

                // Convert decrypted byte array back to string (assuming the original data was encoded in windows-1251)
                return Encoding.GetEncoding("windows-1251").GetString(decryptedData);
            }
        }
        catch (Exception ex)
        {
            return Error.Conflict("9", "Не удалось расшифровать входящий параметр");
        }
    }

    public static Dictionary<string, string> GetParameters(this IQueryCollection parameters)
    {
        Dictionary<string, string> prs = new();

        foreach (var parameter in parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Value))
                continue;

            prs.Add(parameter.Key.ToLower(), parameter.Value!);
        }

        return prs;
    }
    public static Dictionary<string, string> GetParams(this string input, Dictionary<int, string> paramsDict)
    {
        var parameters = input.Split(';');
        Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
        foreach (var param in parameters)
        {
            var p = param.Split(' ');
            var key = paramsDict.TryGetValue(int.Parse(p[0]), out string value) ? value : "";

            keyValuePairs.Add(key, param.Substring(p[0].Length + 1));
        }

        if(!keyValuePairs.TryGetValue("sender_lastname", out string senderLastname) && keyValuePairs.TryGetValue("sender_fullname", out string senderFullname))
        {
            var fio = senderFullname.Split(" ");
            keyValuePairs["sender_lastname"] = fio[0];
            if (fio.Length > 1)
                keyValuePairs["sender_firstname"] = fio[1];
            if (fio.Length > 2)
                keyValuePairs["sender_middlename"] = fio[2];
        }


        return keyValuePairs;
    }

    public static TillabuyTrn MapToTransaction(this IQueryCollection parameters, Dictionary<string, string> agents, Dictionary<string, string> terms, Dictionary<string, string> termsCurrency, Dictionary<int, string> paramsDict)
    {
        var transaction = new TillabuyTrn();

        foreach (var parameter in parameters)
        {
            if (string.IsNullOrEmpty(parameter.Value))
                continue;

            switch (parameter.Key.ToLower())
            {
                case "paymsubjtp":
                    transaction.Service = parameter.Value!;
                    break;
                case "params":
                    var ps = parameter.Value!.ToString().GetParams(paramsDict);
                    transaction.Account = ps["account"];
                    transaction.Parameters = ps.Where(x => x.Key != "account").ToDictionary(x => x.Key, x => x.Value);
                    transaction.SenderName = ps["sender_fullname"];
                    break;
                case "termtype":
                    transaction.TerminalType = parameter.Value!;
                    break;
                case "termid":
                    if(!terms.TryGetValue(parameter.Value!, out string term))
                    {
                        throw new KeyNotFoundException($"TermId {parameter.Value} not found in terms mapping.");
                    }

                    var agent = agents[term];
                    var currency = termsCurrency[term];

                    transaction.Agent = agent;
                    transaction.PayoutCurrency = currency;
                    transaction.TerminalId = term;
                    break;
                case "paymextid":
                    transaction.PaymentExtId = parameter.Value!;
                    break;
                case "amount":
                    transaction.Amount = int.Parse(parameter.Value!);
                    break;
                case "feesum":
                    transaction.FeeSum = decimal.Parse(parameter.Value.ToString().Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." });
                    break;
                case "termtime":
                    transaction.TermExtTime = parameter.Value!;
                    transaction.TermTime = DateTime.Now;
                    break;
                case "requestid":
                    transaction.RequestId = parameter.Value!;
                    break;
            }
        }

        return transaction;
    }

    public static NKOCheckResponse MapToResponse(this ConfirmResponseDto resp, IReadOnlyDictionary<string, string> resolvedParameters, decimal? rate)
    {
        if (resp.Status == "SUCCESS")
            return GetSuccess(resp.ExternalId, resp.Limits!.Remaining!.Amount, resolvedParameters, rate, resp.Credit);
        else if (resp.Status == "PREPARED" || resp.Status == "CONFIRMED")
            return GetPending();

        return GetError();
    }

    public static NKOCheckResponse MapToResponse(this PrepareResponseDto resp)
    {
        if (resp.Status == "SUCCESS")
            return GetSuccess(resp.ExternalId, resp.Limits!.Remaining!.Amount, resp.ResolvedParameters, resp.Rate, resp.Credit);
        else if (resp.Status == "PREPARED" || resp.Status == "CONFIRMED")
            return GetPending();

        return GetError();
    }

    private static NKOCheckResponse GetSuccess(string externalId, long balance, IReadOnlyDictionary<string, string> resolvedParameters, decimal? rate, MoneyDto credit)
    {
        var response = new NKOCheckResponse
        {
            Result = "OK",
            ErrCode = 0,
            PaymExtId = externalId,
            Description  = "Оплата разрешена",
            Balance = balance / 100m
        };

        if (resolvedParameters.Count > 0)
        {
            if (resolvedParameters.TryGetValue("OWNER_CARD", out var customerFIO))
            {
                var extInfo = new ExtInfo()
                {
                    Tags = new CheckTag[]
                    {
                        new CheckTag
                        {
                            Name = "customerFIO",
                            Value = customerFIO
                        }
                    }
                };

                response.ExtInfo = extInfo;
            }
        }

        if (rate.HasValue && rate.Value != 1)
        {
            response.ExchangeRate = rate.Value;
            response.Currency = credit.Currency;
            response.CreditAmount = credit.Amount;
        }

        return response;
    }

    private static NKOCheckResponse GetPending()
    {
        return new NKOCheckResponse
        {
            Result = "Error",
            ErrCode = 15,
            Description  = "Не получен ответ от ИС Получателя за установленный регламентом интервал времени.",
            Balance = 0
        };
    }

    private static NKOCheckResponse GetError()
    {
        return new NKOCheckResponse
        {
            Result = "Error",
            ErrCode = 14,
            Description  = "Отказ ИС Получателя в приеме платежа.",
            Balance = 0
        };
    }
}
