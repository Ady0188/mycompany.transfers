using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Rates.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Rates.Queries;

public sealed record GetFxRateByKeyForAdminQuery(string AgentId, string BaseCurrency, string QuoteCurrency) : IRequest<ErrorOr<FxRateAdminDto>>;

public sealed class GetFxRateByKeyForAdminQueryHandler : IRequestHandler<GetFxRateByKeyForAdminQuery, ErrorOr<FxRateAdminDto>>
{
    private readonly IFxRateRepository _fxRates;

    public GetFxRateByKeyForAdminQueryHandler(IFxRateRepository fxRates) => _fxRates = fxRates;

    public async Task<ErrorOr<FxRateAdminDto>> Handle(GetFxRateByKeyForAdminQuery request, CancellationToken ct)
    {
        var baseCcy = request.BaseCurrency.Trim().ToUpperInvariant();
        var quoteCcy = request.QuoteCurrency.Trim().ToUpperInvariant();
        var entity = await _fxRates.GetForUpdateAsync(request.AgentId, baseCcy, quoteCcy, ct);
        if (entity == null)
            return Error.NotFound(description: "Курс валют не найден.");
        return FxRateAdminDto.FromDomain(entity);
    }
}
