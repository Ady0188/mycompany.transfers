using ErrorOr;
using MyCompany.Transfers.Contract.Solidarnost;

namespace MyCompany.Transfers.Api.Helpers;

/// <summary>
/// Маппинг ErrorOr ошибок (из Command/Query) в Solidarnost XML ответы по спецификации.
/// </summary>
public static class SolidarnostErrorMapper
{
    public static string Map(Error error, string? action = null)
    {
        // Спец-случай: paycheck — если не найдено, возвращаем 707
        if (string.Equals(action, "paycheck", StringComparison.OrdinalIgnoreCase) &&
            error.Type == ErrorType.NotFound)
        {
            return SolidarnostErrorCodes.PaymentNotFound;
        }

        // Подпись / авторизация
        if (error.Code == "auth.bad_signature" || error.Type == ErrorType.Unauthorized)
            return SolidarnostErrorCodes.InvalidSign;

        if (error.Type == ErrorType.Forbidden)
            return SolidarnostErrorCodes.TechnicalForbidden;

        // Дубликаты (ExternalId conflict)
        if (error.Code == "transfer.external_id_conflict" || (error.Type == ErrorType.Conflict && Contains(error.Description, "уже существует")))
            return SolidarnostErrorCodes.Dublicate;

        // Not found: для nmtcheck/clientcheck чаще это "получатель не найден"
        if (error.Type == ErrorType.NotFound)
            return SolidarnostErrorCodes.ReceiverNotFound;

        // Валидации: сумма / аккаунт / дата / pay_id и т.п.
        if (error.Type == ErrorType.Validation)
        {
            var d = error.Description ?? string.Empty;

            if (Contains(d, "подпись"))
                return SolidarnostErrorCodes.InvalidSign;

            if (Contains(d, "валют") || Contains(d, "курс") || Contains(d, "FX rate"))
                return SolidarnostErrorCodes.RateChanged;

            if (Contains(d, "pay") && Contains(d, "date") || Contains(d, "дата"))
                return SolidarnostErrorCodes.InvalidPayDate;

            if (Contains(d, "pay") && Contains(d, "id") || Contains(d, "идентификатор") && Contains(d, "транзакц"))
                return SolidarnostErrorCodes.InvalidPayId;

            if (Contains(d, "сумм") && Contains(d, "слишком мала"))
                return SolidarnostErrorCodes.InvalidAmountLaw;

            if (Contains(d, "сумм") && Contains(d, "слишком велика"))
                return SolidarnostErrorCodes.InvalidAmountMax;

            if (Contains(d, "сумм"))
                return SolidarnostErrorCodes.InvalidAmount;

            if (Contains(d, "счет") && Contains(d, "не актив"))
                return SolidarnostErrorCodes.ReceiverAccountInactive;

            if (Contains(d, "сч") || Contains(d, "account") || Contains(d, "формат"))
                return SolidarnostErrorCodes.InvalidAccount;
        }

        // Остальное
        return SolidarnostErrorCodes.InternalError;
    }

    private static bool Contains(string? text, string part)
        => !string.IsNullOrEmpty(text) && text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;
}

