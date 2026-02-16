using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Accounts;
using MyCompany.Transfers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class AccountDefinitionRepository : IAccountDefinitionRepository
{
    private readonly AppDbContext _db;

    public AccountDefinitionRepository(AppDbContext db) => _db = db;

    public Task<AccountDefinition?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.AccountDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<AccountDefinition>> GetAllAsync(CancellationToken ct = default) =>
        await _db.AccountDefinitions.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);

    public Task<AccountDefinition?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        string.IsNullOrWhiteSpace(code)
            ? Task.FromResult<AccountDefinition?>(null)
            : _db.AccountDefinitions.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code, ct);
}
