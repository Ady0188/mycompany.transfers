namespace MyCompany.Transfers.Domain.Common;

public interface ICurrencyRegistry
{
    int GetExponent(string currency); // TJS→2, RUB→2, JPY→0 и т.д.
}