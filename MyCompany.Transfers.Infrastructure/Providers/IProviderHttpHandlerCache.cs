using MyCompany.Transfers.Domain.Providers;

namespace MyCompany.Transfers.Infrastructure.Providers;

public interface IProviderHttpHandlerCache
{
    void Invalidate(string providerId);
    HttpMessageHandler GetOrCreate(string providerId, ProviderSettings settings);
}
