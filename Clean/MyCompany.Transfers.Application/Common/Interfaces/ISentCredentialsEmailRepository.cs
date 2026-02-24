using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface ISentCredentialsEmailRepository
{
    void Add(SentCredentialsEmail record);
    Task<IReadOnlyList<SentCredentialsEmail>> GetByAgentIdAsync(string agentId, CancellationToken ct = default);
}
