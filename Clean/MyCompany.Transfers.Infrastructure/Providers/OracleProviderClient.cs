using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Infrastructure.Providers;

/// <summary>
/// Stub for IBT (Oracle) provider. Configure Oracle and IDbOracleConnectionFactory to enable.
/// </summary>
public sealed class OracleProviderClient : IProviderClient
{
    public string ProviderId => "IBT";

    public Task<ProviderResult> SendAsync(Provider p, ProviderRequest request, CancellationToken ct)
    {
        return Task.FromResult(new ProviderResult(
            OutboxStatus.SETTING,
            new Dictionary<string, string>(),
            "IBT (Oracle) provider is not configured in Clean. Add IDbOracleConnectionFactory and OracleProviderClient implementation."));
    }
}
