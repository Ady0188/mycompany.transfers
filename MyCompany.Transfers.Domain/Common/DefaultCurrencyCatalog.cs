namespace MyCompany.Transfers.Domain.Common;

public sealed class DefaultCurrencyCatalog : ICurrencyCatalog
{
    private static readonly Dictionary<string, CurrencyInfo> _map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["TJS"] = new("TJS", 2),
            ["USD"] = new("USD", 2),
            ["RUB"] = new("RUB", 2),
            // расширяйте по мере надобности
        };

    public CurrencyInfo this[string code] =>
        _map.TryGetValue(code, out var info)
            ? info
            : throw new DomainException($"Валюта '{code}' не поддерживается.");

    public int GetMinorUnit(string c) => c.ToUpperInvariant() switch
    {
        "JPY" => 0,
        "KWD" => 3,
        _ => 2
    };
}