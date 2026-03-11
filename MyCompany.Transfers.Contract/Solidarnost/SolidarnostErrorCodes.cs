namespace MyCompany.Transfers.Contract.Solidarnost;

/// <summary>
/// Коды ошибок протокола Solidarnost (идентичны Solidarnost.Api).
/// </summary>
public static class SolidarnostErrorCodes
{
    public static readonly string InvalidService = "<Response><CODE>1</CODE><MESSAGE>Временная ошибка. Повторите запрос позже.</MESSAGE></Response>";
    public static readonly string InvalidRequest = "<Response><CODE>2</CODE><MESSAGE>Неизвестный тип запроса</MESSAGE></Response>";
    public static readonly string InvalidSign = "<Response><CODE>14</CODE><MESSAGE>Ошибка подписи</MESSAGE></Response>";
    public static readonly string InvalidAccount = "<Response><CODE>4</CODE><MESSAGE>Неверный формат идентификатора получателя</MESSAGE></Response>";
    public static readonly string InvalidAmount = "<Response><CODE>9</CODE><MESSAGE>Неверная сумма перевода</MESSAGE></Response>";
    public static readonly string InvalidAmountLaw = "<Response><CODE>10</CODE><MESSAGE>Сумма слишком мала</MESSAGE></Response>";
    public static readonly string InvalidAmountMax = "<Response><CODE>11</CODE><MESSAGE>Сумма слишком велика</MESSAGE></Response>";
    public static readonly string Dublicate = "<Response><CODE>8</CODE><MESSAGE>Дублирование транзакции</MESSAGE></Response>";
    public static readonly string InternalError = "<Response><CODE>300</CODE><MESSAGE>Внутренняя ошибка Организации</MESSAGE></Response>";
    public static readonly string PaymentNotFound = "<Response><CODE>707</CODE><MESSAGE>Request payment not received</MESSAGE></Response>";
}
