using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Bins.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Bins.Queries;

public sealed record GetBinByIdQuery(Guid Id) : IRequest<ErrorOr<BinAdminDto>>;

public sealed class GetBinByIdQueryHandler : IRequestHandler<GetBinByIdQuery, ErrorOr<BinAdminDto>>
{
    private readonly IBinRepository _bins;

    public GetBinByIdQueryHandler(IBinRepository bins) => _bins = bins;

    public async Task<ErrorOr<BinAdminDto>> Handle(GetBinByIdQuery request, CancellationToken ct)
    {
        var bin = await _bins.GetForUpdateAsync(request.Id, ct);
        if (bin is null)
            return AppErrors.Common.NotFound($"БИН с Id '{request.Id}' не найден.");
        return BinAdminDto.FromDomain(bin);
    }
}
