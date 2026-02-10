using MyCompany.Transfers.Domain.Accounts;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IAccountDefinitionRepository
{
    Task<AccountDefinition?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AccountDefinition>> GetAllAsync(CancellationToken ct = default);
    Task<AccountDefinition?> GetByCodeAsync(string code, CancellationToken ct = default);
}
