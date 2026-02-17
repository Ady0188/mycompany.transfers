using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Providers;

namespace MyCompany.Transfers.Application.Providers.Queries;

public sealed record GetProviderByIdQuery(string ProviderId) : IRequest<ErrorOr<Provider>>;

public sealed class GetProviderByIdQueryHandler : IRequestHandler<GetProviderByIdQuery, ErrorOr<Provider>>
{
    private readonly IProviderRepository _providers;

    public GetProviderByIdQueryHandler(IProviderRepository providers) => _providers = providers;

    public async Task<ErrorOr<Provider>> Handle(GetProviderByIdQuery request, CancellationToken ct)
    {
        var provider = await _providers.GetAsync(request.ProviderId, ct);
        if (provider is null)
            return AppErrors.Common.NotFound($"Провайдер '{request.ProviderId}' не найден.");
        return provider;
    }
}
