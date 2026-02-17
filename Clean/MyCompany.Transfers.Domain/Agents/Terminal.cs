using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Agents;

public sealed class Terminal : IEntity
{
    public string Id { get; private set; } = default!;
    public string AgentId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string ApiKey { get; private set; } = default!;
    public string Secret { get; private set; } = default!;
    public bool Active { get; private set; }

    private Terminal() { }

    public Terminal(string id, string agentId, string name, string apiKey, string secret, bool active = true)
    {
        Id = id;
        AgentId = agentId;
        Name = name;
        ApiKey = apiKey;
        Secret = secret;
        Active = active;
    }

    /// <summary>
    /// Фабрика создания терминала (DDD).
    /// </summary>
    public static Terminal Create(string id, string agentId, string name, string apiKey, string secret, bool active = true)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Id терминала обязателен.");
        if (string.IsNullOrWhiteSpace(agentId))
            throw new DomainException("AgentId обязателен.");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new DomainException("ApiKey обязателен.");
        return new Terminal(id, agentId, name ?? id, apiKey, secret ?? "", active);
    }

    /// <summary>
    /// Обновление профиля терминала.
    /// </summary>
    public void UpdateProfile(string? agentId = null, string? name = null, string? apiKey = null, string? secret = null, bool? active = null)
    {
        if (!string.IsNullOrWhiteSpace(agentId)) AgentId = agentId;
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        if (!string.IsNullOrWhiteSpace(apiKey)) ApiKey = apiKey;
        if (secret is not null) Secret = secret;
        if (active.HasValue) Active = active.Value;
    }
}
