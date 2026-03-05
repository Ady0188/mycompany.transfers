using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class AccessRepository : IAccessRepository
{
    private readonly AppDbContext _db;

    public AccessRepository(AppDbContext db) => _db = db;

    public Task<bool> IsServiceAllowedAsync(string agentId, string serviceId, CancellationToken ct) =>
        _db.AgentServices.AsNoTracking().AnyAsync(x => x.AgentId == agentId && x.ServiceId == serviceId && x.Enabled, ct);

    public Task<bool> IsCurrencyAllowedAsync(string agentId, string currency, CancellationToken ct)
    {
        var normCurrency = currency.Trim().ToUpperInvariant();
        return _db.Terminals.AsNoTracking()
            .AnyAsync(x => x.AgentId == agentId && x.Currency == normCurrency && x.Active, ct);
    }

    public Task<List<string>> GetAllowedCurrenciesAsync(string agentId, CancellationToken ct) =>
        _db.Terminals.AsNoTracking()
            .Where(x => x.AgentId == agentId && x.Active)
            .Select(x => x.Currency)
            .Distinct()
            .ToListAsync(ct);

    public Task<AgentServiceAccess?> GetAgentServiceAccessAsync(string agentId, string serviceId, CancellationToken ct) =>
        _db.AgentServices.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AgentId == agentId && x.ServiceId == serviceId && x.Enabled, ct);

    public Task<bool> AnyByAgentIdAsync(string agentId, CancellationToken ct) =>
        _db.AgentServices.AnyAsync(x => x.AgentId == agentId, ct);

    public Task<bool> AnyByServiceIdAsync(string serviceId, CancellationToken ct) =>
        _db.AgentServices.AnyAsync(x => x.ServiceId == serviceId, ct);

    public async Task<IReadOnlyList<AgentServiceAccess>> GetAllAgentServiceAccessAsync(CancellationToken ct) =>
        await _db.AgentServices.AsNoTracking().OrderBy(x => x.AgentId).ThenBy(x => x.ServiceId).ToListAsync(ct).ConfigureAwait(false);

    public Task<AgentServiceAccess?> GetAgentServiceAccessForUpdateAsync(string agentId, string serviceId, CancellationToken ct) =>
        _db.AgentServices.FirstOrDefaultAsync(x => x.AgentId == agentId && x.ServiceId == serviceId, ct);

    public Task<bool> ExistsAgentServiceAccessAsync(string agentId, string serviceId, CancellationToken ct) =>
        _db.AgentServices.AnyAsync(x => x.AgentId == agentId && x.ServiceId == serviceId, ct);

    public void Add(AgentServiceAccess entity) => _db.AgentServices.Add(entity);
    public void Update(AgentServiceAccess entity) => _db.AgentServices.Update(entity);
    public void Remove(AgentServiceAccess entity) => _db.AgentServices.Remove(entity);
}
