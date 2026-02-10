namespace MyCompany.Transfers.Domain.Helpers;

public static class MoneyConvert
{
    public static decimal MinorToDecimal(this long minor, int exp)
        => exp == 0 ? minor : minor / (decimal)Math.Pow(10, exp);

    public static long DecimalToMinor(this decimal amount, int exp)
        => exp == 0 ? (long)amount : (long)Math.Round(amount * (decimal)Math.Pow(10, exp), 0);
}