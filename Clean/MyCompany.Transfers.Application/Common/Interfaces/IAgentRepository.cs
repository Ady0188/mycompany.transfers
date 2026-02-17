using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Common.Interfaces;

/// <summary>
/// Полный репозиторий агентов для административных операций (CRUD).
/// Для бизнес-операций чтения по-прежнему используется IAgentReadRepository (с кэшем).
/// </summary>
public interface IAgentRepository : IAgentReadRepository
{
    Task<IReadOnlyList<Agent>> GetAllAsync(CancellationToken ct);
    void Add(Agent agent);
    void Update(Agent agent);
    void Remove(Agent agent);
}

