using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class CurrencyConverter : ICurrencyConverter
{
    public long ConvertMinor(long srcMinor, int srcMinorUnit, int dstMinorUnit, decimal rate, string rounding = "floor")
    {
        // Преобразуем в major исходной валюты
        var srcMajor = srcMinor / (decimal)Math.Pow(10, srcMinorUnit);

        // Пересчитываем в major целевой валюты
        var dstMajor = srcMajor * rate;

        // Переводим в minor целевой валюты
        var factor = (decimal)Math.Pow(10, dstMinorUnit);
        var dstMinorRaw = dstMajor * factor;

        decimal dstMinorRounded = rounding switch
        {
            "ceil" => Math.Ceiling(dstMinorRaw),
            "bankers" => Math.Round(dstMinorRaw, 0, MidpointRounding.ToEven),
            _ => Math.Floor(dstMinorRaw)
        };

        return (long)dstMinorRounded;
    }
}