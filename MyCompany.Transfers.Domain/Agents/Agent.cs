using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Agents;

public sealed class Agent : IAggregateRoot
{
    public string Id { get; private set; } = default!;
    public string TimeZoneId { get; set; } = "Asia/Dushanbe";
    public Dictionary<string, long> Balances { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public string SettingsJson { get; private set; } = "{}";

    private Agent() { }
    public Agent(string id) => Id = id;

    public bool HasSufficientBalance(string currency, long amountMinor)
        => Balances.TryGetValue(currency, out var v) && 
            v >= amountMinor;

    public void Credit(string currency, long minor)
        => Balances[currency] = (Balances.TryGetValue(currency, out var v) ? v : 0) + minor;

    public void Debit(string currency, long minor)
    {
        if (!HasSufficientBalance(currency, minor))
            throw new DomainException("Insufficient balance");

        Balances[currency] -= minor;
    }
}