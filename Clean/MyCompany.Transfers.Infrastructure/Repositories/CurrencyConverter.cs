using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class CurrencyConverter : ICurrencyConverter
{
    public long ConvertMinor(long srcMinor, int srcMinorUnit, int dstMinorUnit, decimal rate, string rounding = "floor")
    {
        var srcMajor = srcMinor / (decimal)Math.Pow(10, srcMinorUnit);
        var dstMajor = srcMajor * rate;
        var factor = (decimal)Math.Pow(10, dstMinorUnit);
        var dstMinorRaw = dstMajor * factor;
        var dstMinorRounded = rounding switch
        {
            "ceil" => Math.Ceiling(dstMinorRaw),
            "bankers" => Math.Round(dstMinorRaw, 0, MidpointRounding.ToEven),
            _ => Math.Floor(dstMinorRaw)
        };
        return (long)dstMinorRounded;
    }
}
