namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface ICurrencyConverter
{
    long ConvertMinor(long srcMinor, int srcMinorUnit, int dstMinorUnit, decimal rate, string rounding = "floor");
}