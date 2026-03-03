using MediatR;
using MyCompany.Transfers.Application.Rates.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Rates.Queries;

/// <summary>
/// Список курсов валют для админ-панели. Опционально по agentId.
/// </summary>
public sealed record GetFxRatesForAdminQuery(string? AgentId = null) : IRequest<IReadOnlyList<FxRateAdminDto>>;

public sealed class GetFxRatesForAdminQueryHandler : IRequestHandler<GetFxRatesForAdminQuery, IReadOnlyList<FxRateAdminDto>>
{
    private readonly IFxRateRepository _fxRates;

    public GetFxRatesForAdminQueryHandler(IFxRateRepository fxRates) => _fxRates = fxRates;

    public async Task<IReadOnlyList<FxRateAdminDto>> Handle(GetFxRatesForAdminQuery request, CancellationToken ct)
    {
        var list = await _fxRates.GetAllForAdminAsync(request.AgentId, ct);
        return list.Select(FxRateAdminDto.FromDomain).ToList();
    }
}
