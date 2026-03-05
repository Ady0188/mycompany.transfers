using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Agents;

/// <summary>
/// Терминал агента. Один терминал = один банковский счёт = одна валюта = один баланс.
/// Для нескольких валют/счетов у агента создают несколько терминалов.
/// </summary>
public sealed class Terminal : IEntity
{
    public string Id { get; private set; } = default!;
    public string AgentId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    /// <summary>Счёт списания в банке для этого терминала (одна валюта на счёт).</summary>
    public string Account { get; private set; } = default!;
    /// <summary>Счёт дохода банка (для проводок дохода по этому терминалу).</summary>
    public string? BankIncomeAccount { get; private set; }
    /// <summary>Валюта счёта и баланса терминала.</summary>
    public string Currency { get; private set; } = default!;
    /// <summary>Баланс в минорных единицах (центы и т.д.).</summary>
    public long BalanceMinor { get; private set; }
    public string ApiKey { get; private set; } = default!;
    public string Secret { get; private set; } = default!;
    public bool Active { get; private set; }

    private Terminal() { }

    public Terminal(string id, string agentId, string name, string account, string? bankIncomeAccount, string currency, long balanceMinor, string apiKey, string secret, bool active = true)
    {
        Id = id;
        AgentId = agentId;
        Name = name;
        Account = account;
        BankIncomeAccount = string.IsNullOrWhiteSpace(bankIncomeAccount) ? null : bankIncomeAccount.Trim();
        Currency = currency;
        BalanceMinor = balanceMinor;
        ApiKey = apiKey;
        Secret = secret;
        Active = active;
    }

    /// <summary>
    /// Фабрика создания терминала (DDD). Один терминал = один счёт в банке с одной валютой и своим балансом.
    /// </summary>
    public static Terminal Create(string id, string agentId, string name, string account, string? bankIncomeAccount, string currency, string apiKey, string secret, bool active = true)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Id терминала обязателен.");
        if (string.IsNullOrWhiteSpace(agentId))
            throw new DomainException("AgentId обязателен.");
        if (string.IsNullOrWhiteSpace(account))
            throw new DomainException("Счёт терминала обязателен для проводок.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Валюта терминала обязательна.");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new DomainException("ApiKey обязателен.");
        var normCurrency = currency.Trim().ToUpperInvariant();
        var incomeAcc = string.IsNullOrWhiteSpace(bankIncomeAccount) ? null : bankIncomeAccount.Trim();
        return new Terminal(id, agentId, name ?? id, account.Trim(), incomeAcc, normCurrency, 0L, apiKey, secret ?? "", active);
    }

    /// <summary>
    /// Обновление профиля терминала.
    /// </summary>
    public void UpdateProfile(string? agentId = null, string? name = null, string? account = null, string? bankIncomeAccount = null, string? currency = null, string? apiKey = null, string? secret = null, bool? active = null)
    {
        if (!string.IsNullOrWhiteSpace(agentId)) AgentId = agentId;
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        if (!string.IsNullOrWhiteSpace(account)) Account = account;
        if (bankIncomeAccount is not null) BankIncomeAccount = string.IsNullOrWhiteSpace(bankIncomeAccount) ? null : bankIncomeAccount.Trim();
        if (!string.IsNullOrWhiteSpace(currency)) Currency = currency.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(apiKey)) ApiKey = apiKey;
        if (secret is not null) Secret = secret;
        if (active.HasValue) Active = active.Value;
    }

    public bool HasSufficientBalance(long amountMinor) => BalanceMinor >= amountMinor;

    public void Credit(long amountMinor)
    {
        BalanceMinor = checked(BalanceMinor + amountMinor);
    }

    public void Debit(long amountMinor)
    {
        if (!HasSufficientBalance(amountMinor))
            throw new DomainException("Insufficient balance");
        BalanceMinor -= amountMinor;
    }
}
