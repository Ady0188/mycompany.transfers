using MediatR;
using MyCompany.Transfers.Application.Parameters.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Parameters.Queries;

public sealed record GetParametersQuery() : IRequest<IReadOnlyList<ParameterAdminDto>>;

public sealed class GetParametersQueryHandler : IRequestHandler<GetParametersQuery, IReadOnlyList<ParameterAdminDto>>
{
    private readonly IParameterRepository _parameters;

    public GetParametersQueryHandler(IParameterRepository parameters) => _parameters = parameters;

    public async Task<IReadOnlyList<ParameterAdminDto>> Handle(GetParametersQuery request, CancellationToken ct)
    {
        var list = await _parameters.GetAllForAdminAsync(ct);
        return list.Select(ParameterAdminDto.FromDomain).ToList();
    }
}
