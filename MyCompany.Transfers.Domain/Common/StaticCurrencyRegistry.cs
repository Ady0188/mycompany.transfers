namespace MyCompany.Transfers.Domain.Common;

public sealed class StaticCurrencyRegistry : ICurrencyRegistry
{
    private static readonly Dictionary<string, int> _exp = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TJS"] = 2,
        ["RUB"] = 2,
        ["USD"] = 2,
        ["EUR"] = 2
    };
    public int GetExponent(string currency) => _exp.TryGetValue(currency, out var e) ? e : 2;
}