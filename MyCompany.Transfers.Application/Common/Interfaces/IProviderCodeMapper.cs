using MyCompany.Transfers.Application.Common.Helpers;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IProviderCodeMapper
{
    Task<ProviderMappingResult> MapAsync(
        string providerId,
        string providerCode,
        CancellationToken ct);
}