using System.Globalization;

namespace MyCompany.Transfers.Contract.Solidarnost;

/// <summary>
/// Формирование XML-ответов протокола Solidarnost (как в Solidarnost.Api).
/// Важно: у некоторых методов корневой тег отличается регистром (Response vs response).
/// </summary>
public static class SolidarnostResponseBuilder
{
    /// <summary>
    /// nmtcheck success (пример из Solidarnost): корневой тег Response, MESSAGE=Account Exists,
    /// и набор полей (FIO, CREDIT_AMOUNT, CREDIT_CURR, CURR_RATE, RECEIVER_*).
    /// </summary>
    public static string NmtCheckSuccess(
        string? fio,
        decimal creditAmount,
        string? creditCurr,
        decimal? currRate,
        string? receiverFee,
        string? receiverExtId,
        string? receiverAccount,
        string? receiverCard)
    {
        var creditAmountStr = creditAmount.ToString("F2", CultureInfo.InvariantCulture);
        var currRateStr = currRate is null ? string.Empty : currRate.Value.ToString("0.####", CultureInfo.InvariantCulture);

        return $@"<Response>
<CODE>0</CODE>
<MESSAGE>Account Exists</MESSAGE>
<FIO>{EscapeXml(fio)}</FIO>
<CREDIT_AMOUNT>{EscapeXml(creditAmountStr)}</CREDIT_AMOUNT>
<CREDIT_CURR>{EscapeXml(creditCurr)}</CREDIT_CURR>
<CURR_RATE>{EscapeXml(currRateStr)}</CURR_RATE>
<RECEIVER_FEE>{EscapeXml(receiverFee)}</RECEIVER_FEE>
<RECEIVER_EXT_ID>{EscapeXml(receiverExtId)}</RECEIVER_EXT_ID>
<RECEIVER_ACCOUNT>{EscapeXml(receiverAccount)}</RECEIVER_ACCOUNT>
<RECEIVER_CARD>{EscapeXml(receiverCard)}</RECEIVER_CARD>
</Response>";
    }

    /// <summary>clientcheck success (пример из Solidarnost): Response, MESSAGE=ClientChecked.</summary>
    public static string ClientCheckSuccess()
    {
        return @"<Response>
<CODE>0</CODE>
<MESSAGE>ClientChecked</MESSAGE>
</Response>";
    }

    /// <summary>payment / paycheck success: как MapToSuccessResponse в Solidarnost (корневой response).</summary>
    public static string PaymentSuccess(string extId, string regDate)
    {
        return $@"<response>
<CODE>0</CODE>
<MESSAGE>Payment Successful</MESSAGE>
<EXT_ID>{EscapeXml(extId)}</EXT_ID>
<REG_DATE>{EscapeXml(regDate)}</REG_DATE>
</response>";
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
