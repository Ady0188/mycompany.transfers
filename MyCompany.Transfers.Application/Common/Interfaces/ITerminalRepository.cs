using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface ITerminalRepository
{
    Task<Terminal?> GetByApiKeyAsync(string apiKey, CancellationToken ct);
    Task<Terminal?> GetAsync(string terminalId, CancellationToken ct);
    Task<Terminal?> GetForUpdateAsync(string terminalId, CancellationToken ct);
    /// <summary>Найти активный терминал агента по валюте (для АБС кредит/дебет по AgentId+Currency).</summary>
    Task<Terminal?> GetByAgentIdAndCurrencyForUpdateAsync(string agentId, string currency, CancellationToken ct);
    Task<bool> ExistsAsync(string terminalId, CancellationToken ct);
    Task<bool> AnyByAgentIdAsync(string agentId, CancellationToken ct);
    Task<IReadOnlyList<Terminal>> GetAllAsync(CancellationToken ct);
    void Add(Terminal terminal);
    void Update(Terminal terminal);
    void Remove(Terminal terminal);
}
