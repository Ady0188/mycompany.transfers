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

    public Task<bool> IsCurrencyAllowedAsync(string agentId, string currency, CancellationToken ct) =>
        _db.AgentCurrencies.AsNoTracking().AnyAsync(x => x.AgentId == agentId && x.Currency == currency && x.Enabled, ct);

    public Task<List<string>> GetAllowedCurrenciesAsync(string agentId, CancellationToken ct) =>
        _db.AgentCurrencies.AsNoTracking()
            .Where(x => x.AgentId == agentId && x.Enabled)
            .Select(x => x.Currency)
            .ToListAsync(ct);

    public Task<AgentServiceAccess?> GetAgentServiceAccessAsync(string agentId, string serviceId, CancellationToken ct) =>
        _db.AgentServices.AsNoTracking()
            .FirstOrDefaultAsync(x => x.AgentId == agentId && x.ServiceId == serviceId && x.Enabled, ct);
}
