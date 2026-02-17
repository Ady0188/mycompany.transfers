using MediatR;
using MyCompany.Transfers.Application.Providers.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Providers.Queries;

public sealed record GetProvidersQuery() : IRequest<IReadOnlyList<ProviderAdminDto>>;

public sealed class GetProvidersQueryHandler : IRequestHandler<GetProvidersQuery, IReadOnlyList<ProviderAdminDto>>
{
    private readonly IProviderRepository _providers;

    public GetProvidersQueryHandler(IProviderRepository providers) => _providers = providers;

    public async Task<IReadOnlyList<ProviderAdminDto>> Handle(GetProvidersQuery request, CancellationToken ct)
    {
        var list = await _providers.GetAllAsync(ct);
        return list.Select(ProviderAdminDto.FromDomain).ToList();
    }
}
