namespace MyCompany.Transfers.Domain.Common;

public interface ICurrencyCatalog
{
    CurrencyInfo this[string code] { get; }
    int GetMinorUnit(string currency);
}