using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Common.Interfaces;
public interface IParameterRepository
{
    Task<IReadOnlyList<ParamDefinition>> GetAllAsync(CancellationToken ct);
    Task<ParamDefinition?> GetByIdAsync(string id, CancellationToken ct);
    Task<ParamDefinition?> GetByCodeAsync(string code, CancellationToken ct);

    // Удобно для валидации услуги: сразу получить map по нужным Id
    Task<Dictionary<string, ParamDefinition>> GetByIdsAsMapAsync(IEnumerable<string> ids, CancellationToken ct);
}