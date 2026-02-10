using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Agents;

public sealed class Terminal : IEntity
{
    public string Id { get; private set; } = default!;
    public string AgentId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string ApiKey { get; private set; } = default!;  // публичный ключ
    public string Secret { get; private set; } = default!;  // секрет
    public bool Active { get; private set; }

    private Terminal() { }

    public Terminal(string id, string agentId, string name, string apiKey, string secret, bool active = true)
    {
        Id = id; AgentId = agentId; Name = name; ApiKey = apiKey; Secret = secret; Active = active;
    }
}