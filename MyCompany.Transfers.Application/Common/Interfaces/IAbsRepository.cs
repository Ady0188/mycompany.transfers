using MyCompany.Transfers.Application.Common.Providers.Responses;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IAbsRepository
{
    Task<AbsCheckResponse?> CheckAbsAsync(string request, CancellationToken cancellationToken = default);
}