using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Parameters.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Parameters.Queries;

public sealed record GetParameterByIdQuery(string ParameterId) : IRequest<ErrorOr<ParameterAdminDto>>;

public sealed class GetParameterByIdQueryHandler : IRequestHandler<GetParameterByIdQuery, ErrorOr<ParameterAdminDto>>
{
    private readonly IParameterRepository _parameters;

    public GetParameterByIdQueryHandler(IParameterRepository parameters) => _parameters = parameters;

    public async Task<ErrorOr<ParameterAdminDto>> Handle(GetParameterByIdQuery request, CancellationToken ct)
    {
        var param = await _parameters.GetForUpdateAsync(request.ParameterId, ct);
        if (param is null)
            return AppErrors.Common.NotFound($"Параметр '{request.ParameterId}' не найден.");
        return ParameterAdminDto.FromDomain(param);
    }
}
