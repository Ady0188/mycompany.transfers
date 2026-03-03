using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IParameterRepository
{
    Task<IReadOnlyList<ParamDefinition>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<ParamDefinition>> GetAllForAdminAsync(CancellationToken ct);
    Task<ParamDefinition?> GetByIdAsync(string id, CancellationToken ct);
    Task<ParamDefinition?> GetForUpdateAsync(string id, CancellationToken ct);
    Task<ParamDefinition?> GetByCodeAsync(string code, CancellationToken ct);
    Task<bool> ExistsAsync(string id, CancellationToken ct);
    /// <summary>Возвращает следующий числовой Id для нового параметра (начиная с 100).</summary>
    Task<string> GetNextNumericIdAsync(CancellationToken ct);
    Task<bool> AnyUsedByServiceAsync(string parameterId, CancellationToken ct);
    Task<Dictionary<string, ParamDefinition>> GetByIdsAsMapAsync(IEnumerable<string> ids, CancellationToken ct);
    void Add(ParamDefinition param);
    void Update(ParamDefinition param);
    void Remove(ParamDefinition param);
}
