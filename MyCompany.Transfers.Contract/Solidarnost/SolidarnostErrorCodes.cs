namespace MyCompany.Transfers.Contract.Solidarnost;

/// <summary>
/// Коды ошибок протокола Solidarnost (идентичны Solidarnost.Api).
/// </summary>
public static class SolidarnostErrorCodes
{
    public static readonly string InvalidService = "<Response><CODE>1</CODE><MESSAGE>Временная ошибка. Повторите запрос позже.</MESSAGE></Response>";
    public static readonly string InvalidRequest = "<Response><CODE>2</CODE><MESSAGE>Неизвестный тип запроса</MESSAGE></Response>";
    public static readonly string ReceiverNotFound = "<Response><CODE>3</CODE><MESSAGE>Получатель не найден</MESSAGE></Response>";
    public static readonly string InvalidSign = "<Response><CODE>14</CODE><MESSAGE>Ошибка подписи</MESSAGE></Response>";
    public static readonly string InvalidAccount = "<Response><CODE>4</CODE><MESSAGE>Неверный формат идентификатора получателя</MESSAGE></Response>";
    public static readonly string ReceiverAccountInactive = "<Response><CODE>5</CODE><MESSAGE>Счет Получателя не активен</MESSAGE></Response>";
    public static readonly string InvalidPayId = "<Response><CODE>6</CODE><MESSAGE>Неверное значение идентификатора транзакции</MESSAGE></Response>";
    public static readonly string TechnicalForbidden = "<Response><CODE>7</CODE><MESSAGE>Прием Перевода запрещен по техническим причинам</MESSAGE></Response>";
    public static readonly string InvalidAmount = "<Response><CODE>9</CODE><MESSAGE>Неверная сумма перевода</MESSAGE></Response>";
    public static readonly string InvalidAmountLaw = "<Response><CODE>10</CODE><MESSAGE>Сумма слишком мала</MESSAGE></Response>";
    public static readonly string InvalidAmountMax = "<Response><CODE>11</CODE><MESSAGE>Сумма слишком велика</MESSAGE></Response>";
    public static readonly string InvalidPayDate = "<Response><CODE>12</CODE><MESSAGE>Неверное значение даты Перевода</MESSAGE></Response>";
    public static readonly string RateChanged = "<Response><CODE>13</CODE><MESSAGE>Курс конверсии изменен</MESSAGE></Response>";
    public static readonly string Dublicate = "<Response><CODE>8</CODE><MESSAGE>Дублирование транзакции</MESSAGE></Response>";
    public static readonly string InternalError = "<Response><CODE>300</CODE><MESSAGE>Внутренняя ошибка Организации</MESSAGE></Response>";

    // Доп. коды по спецификации (payment/paycheck)
    public static readonly string CardDataNotFound = "<Response><CODE>700</CODE><MESSAGE>card data not found</MESSAGE></Response>";
    public static readonly string NoIdentLimit = "<Response><CODE>701</CODE><MESSAGE>No ident limit</MESSAGE></Response>";
    public static readonly string BlockedCard = "<Response><CODE>703</CODE><MESSAGE>blocked card</MESSAGE></Response>";
    public static readonly string UserSleepedOrBlocked = "<Response><CODE>704</CODE><MESSAGE>user sleeped or blocked</MESSAGE></Response>";
    public static readonly string RatesError = "<Response><CODE>705</CODE><MESSAGE>rates error</MESSAGE></Response>";
    public static readonly string CreditError = "<Response><CODE>706</CODE><MESSAGE>Credit error</MESSAGE></Response>";

    public static readonly string PaymentNotFound = "<Response><CODE>707</CODE><MESSAGE>Request payment not received</MESSAGE></Response>";
}
