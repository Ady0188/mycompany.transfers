using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ErrorOr;
using MyCompany.Transfers.Contract.Tillabuy.Requests;
using MyCompany.Transfers.Contract.Tillabuy.Responses;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Api.Helpers;

/// <summary>
/// Коды ошибок и тексты по протоколу Tillabuy (НКО).
/// </summary>
public static class TillabuyExtensions
{
    public static readonly Dictionary<int, string> Errors = new()
    {
        { 0, "OK" },
        { 2, "Пункт приема платежа не зарегистрирован или блокирован, либо в запросе указано некорректное значение TermType." },
        { 4, "Отсутствует параметр PaymExtId или не задано его значение; при отправке запроса использован метод, отличный от GET." },
        { 5, "Код назначения платежа, указанный в параметре PaymSubjTp, не зарегистрирован в Системе." },
        { 8, "Значения параметров Params, Amount, TermType, PaymExtId, TermId, TermTime не соответствуют установленным шаблонам или требованиям для данного кода назначения." },
        { 9, "Временная проблема с обработкой запроса на стороне биллинга МБТ." },
        { 10, "Сумма платежа, переданная в параметре Amount, не соответствует установленным для данного кода назначения ограничениям." },
        { 14, "Платеж по данной операции уже создан, находится в состоянии Не подготовлен." },
        { 41, "Сумма платежа, переданная в параметре Amount, отличается от аналогичной суммы, переданной в исходном запросе." },
        { 42, "Значение, переданное в параметрах PaymSubjTp, Params или TermType, отличается от аналогичных значений, переданных в исходном запросе." },
        { 30, "Недостаточно денежных средств на балансе Банка в МБТ" },
        { 141, "Отказ ИС Получателя в приеме платежа." },
        { 142, "Система банка партнера временно не доступна, повторите попытку чуть позже." },
        { 143, "Платеж по данной операции не найден." },
        { 144, "Время на подтверждение перевода истекло" },
        { 55, "Нарушены правила финансового или fraud-мониторинга." },
        { 15, "Не получен ответ от ИС Получателя за установленный регламентом интервал времени." }
    };

    /// <summary>Маппинг кодов ошибок API (ErrorOr) в коды протокола Tillabuy.</summary>
    public static readonly Dictionary<string, int> ApiErrors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "OK", 0 },
        { "common.unexpected", 9 },
        { "common.validation", 8 },
        { "auth.unauthorized", 2 },
        { "auth.forbidden", 55 },
        { "common.not_found", 143 },
        { "common.invalid_request", 4 },
        { "transfer.not_found", 143 },
        { "transfer.not_prepared", 14 },
        { "transfer.already_finished", 14 },
        { "transfer.external_id_conflict", 14 },
        { "transfer.already_confirmed", 14 },
        { "transfer.quote_mismatch", 42 },
        { "transfer.quote_expired", 144 },
        { "transfer.invalid_request", 8 },
        { "agent.not_found", 2 },
        { "agent.insufficient_balance", 30 },
        { "auth.bad_signature", 2 },
        { "auth.signature_expired", 2 },
        { "auth.terminal_not_found", 2 }
    };

    public static Dictionary<string, string> GetParameters(this IQueryCollection parameters)
    {
        var prs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Value)) continue;
            prs[parameter.Key.ToLowerInvariant()] = parameter.Value!;
        }
        return prs;
    }

    /// <summary>
    /// Расшифровка параметра (например, PAN карты), зашифрованного RSA OAEP SHA-1.
    /// Поддерживается ключ в .key (PEM) или сертификат .pfx.
    /// </summary>
    public static ErrorOr<string> DecryptParameter(this string input, string privateKeyPath, string? privateKeyPass = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input))
                return Error.Conflict("9", "Не удалось расшифровать входящий параметр");

            var encryptedData = Convert.FromBase64String(Uri.UnescapeDataString(input));

            if (privateKeyPath.EndsWith(".key", StringComparison.OrdinalIgnoreCase))
            {
                var privateKeyPem = File.ReadAllText(privateKeyPath);
                using var rsa = RSA.Create();
                rsa.ImportFromEncryptedPem(privateKeyPem, privateKeyPass ?? ReadOnlySpan<char>.Empty);
                var decryptedData = rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA1);
                return Encoding.GetEncoding("windows-1251").GetString(decryptedData);
            }

#pragma warning disable SYSLIB0057
            var cert = new X509Certificate2(privateKeyPath, privateKeyPass ?? "", X509KeyStorageFlags.Exportable);
#pragma warning restore SYSLIB0057
            using var rsaCert = cert.GetRSAPrivateKey();
            if (rsaCert is null)
                return Error.Conflict("9", "Не удалось получить закрытый ключ из сертификата");
            var decrypted = rsaCert.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA1);
            return Encoding.GetEncoding("windows-1251").GetString(decrypted);
        }
        catch (Exception)
        {
            return Error.Conflict("9", "Не удалось расшифровать входящий параметр");
        }
    }

    private static readonly Dictionary<int, string> ParamsDict = new()
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

    private static Dictionary<string, string> GetParamsFromString(string input)
    {
        var parameters = input.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var keyValuePairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var param in parameters)
        {
            var spaceIdx = param.IndexOf(' ');
            if (spaceIdx <= 0) continue;
            var keyNum = param.AsSpan(0, spaceIdx);
            if (!int.TryParse(keyNum, out var keyId) || !ParamsDict.TryGetValue(keyId, out var key))
                continue;
            keyValuePairs[key] = param.Substring(spaceIdx + 1).Trim();
        }
        if (!keyValuePairs.ContainsKey("sender_lastname") && keyValuePairs.TryGetValue("sender_fullname", out var fullname))
        {
            var fio = fullname.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            keyValuePairs["sender_lastname"] = fio.Length > 0 ? fio[0] : "";
            if (fio.Length > 1) keyValuePairs["sender_firstname"] = fio[1];
            if (fio.Length > 2) keyValuePairs["sender_middlename"] = fio[2];
        }
        return keyValuePairs;
    }

    public static TillabuyTrn MapToTransaction(
        this IQueryCollection parameters,
        Dictionary<string, string> agents,
        Dictionary<string, string> terms,
        Dictionary<string, string> termsCurrency,
        string defaultCurrency = "TJS")
    {
        var transaction = new TillabuyTrn();
        var prs = parameters.GetParameters();

        foreach (var (key, value) in prs)
        {
            switch (key)
            {
                case "paymsubjtp":
                    transaction.Service = value;
                    break;
                case "params":
                    var ps = GetParamsFromString(value);
                    transaction.Account = ps.GetValueOrDefault("account", "");
                    transaction.Parameters = ps.Where(x => !string.Equals(x.Key, "account", StringComparison.OrdinalIgnoreCase)).ToDictionary(x => x.Key, x => x.Value);
                    transaction.SenderName = ps.GetValueOrDefault("sender_fullname", "");
                    break;
                case "termtype":
                    transaction.TerminalType = value;
                    break;
                case "termid":
                    if (!terms.TryGetValue(value, out var termKey))
                        throw new KeyNotFoundException($"TermId {value} not found in terms mapping.");
                    transaction.Agent = agents.TryGetValue(termKey, out var agent) ? agent : "";
                    transaction.PayoutCurrency = termsCurrency.TryGetValue(termKey, out var currency) ? currency : defaultCurrency;
                    transaction.TerminalId = termKey;
                    break;
                case "paymextid":
                    transaction.PaymentExtId = value;
                    break;
                case "amount":
                    if (long.TryParse(value, out var amount))
                        transaction.Amount = amount;
                    break;
                case "feesum":
                    transaction.FeeSum = decimal.Parse(value.Replace(",", "."), NumberFormatInfo.InvariantInfo);
                    break;
                case "termtime":
                    transaction.TermExtTime = value;
                    transaction.TermTime = DateTime.Now;
                    break;
                case "requestid":
                    transaction.RequestId = value;
                    break;
            }
        }
        return transaction;
    }

    public static NKOCheckResponse MapToResponse(this PrepareResponseDto resp)
    {
        if (string.Equals(resp.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            return BuildNkoSuccess(resp.ExternalId ?? "", resp.Limits?.Remaining?.Amount ?? 0, resp.ResolvedParameters ?? new Dictionary<string, string>(), resp.Rate, resp.Credit);
        if (string.Equals(resp.Status, "PREPARED", StringComparison.OrdinalIgnoreCase) || string.Equals(resp.Status, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
            return BuildNkoPending();
        return BuildNkoError();
    }

    public static NKOCheckResponse MapToResponse(this ConfirmResponseDto resp, IReadOnlyDictionary<string, string>? resolvedParameters, decimal? rate, MoneyDto? credit)
    {
        if (string.Equals(resp.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            return BuildNkoSuccess(resp.ExternalId ?? "", resp.Limits?.Remaining?.Amount ?? 0, resolvedParameters ?? new Dictionary<string, string>(), rate, credit);
        if (string.Equals(resp.Status, "PREPARED", StringComparison.OrdinalIgnoreCase) || string.Equals(resp.Status, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
            return BuildNkoPending();
        return BuildNkoError();
    }

    private static NKOCheckResponse BuildNkoSuccess(string externalId, long balanceMinor, IReadOnlyDictionary<string, string> resolvedParameters, decimal? rate, MoneyDto? credit)
    {
        var response = new NKOCheckResponse
        {
            Result = "OK",
            ErrCode = 0,
            PaymExtId = externalId,
            Description = "Оплата разрешена",
            Balance = balanceMinor / 100m
        };
        if (resolvedParameters.TryGetValue("OWNER_CARD", out var customerFio))
            response.ExtInfo = new ExtInfo { Tags = new[] { new CheckTag { Name = "customerFIO", Value = customerFio } } };
        if (rate.HasValue && rate.Value != 1 && credit is not null)
        {
            response.ExchangeRate = rate.Value;
            response.Currency = credit.Currency;
            response.CreditAmount = credit.Amount;
        }
        return response;
    }

    private static NKOCheckResponse BuildNkoPending() => new()
    {
        Result = "Error",
        ErrCode = 15,
        Description = Errors[15],
        Balance = 0
    };

    private static NKOCheckResponse BuildNkoError() => new()
    {
        Result = "Error",
        ErrCode = 14,
        Description = Errors[14],
        Balance = 0
    };

    public static int ToTillabuyErrorCode(this Error error) =>
        ApiErrors.TryGetValue(error.Code, out var code) ? code : 14;
}
