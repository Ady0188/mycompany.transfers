using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface ITerminalRepository
{
    Task<Terminal?> GetByApiKeyAsync(string apiKey, CancellationToken ct);
}