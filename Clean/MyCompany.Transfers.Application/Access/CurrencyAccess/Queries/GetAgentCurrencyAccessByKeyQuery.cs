using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Access.CurrencyAccess.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Access.CurrencyAccess.Queries;

public sealed record GetAgentCurrencyAccessByKeyQuery(string AgentId, string Currency) : IRequest<ErrorOr<AgentCurrencyAccessAdminDto>>;

public sealed class GetAgentCurrencyAccessByKeyQueryHandler : IRequestHandler<GetAgentCurrencyAccessByKeyQuery, ErrorOr<AgentCurrencyAccessAdminDto>>
{
    private readonly IAccessRepository _access;

    public GetAgentCurrencyAccessByKeyQueryHandler(IAccessRepository access) => _access = access;

    public async Task<ErrorOr<AgentCurrencyAccessAdminDto>> Handle(GetAgentCurrencyAccessByKeyQuery request, CancellationToken ct)
    {
        var entity = await _access.GetAgentCurrencyAccessForUpdateAsync(request.AgentId, request.Currency, ct);
        if (entity == null)
            return Error.NotFound(description: "Запись доступа агент–валюта не найдена.");
        return AgentCurrencyAccessAdminDto.FromDomain(entity);
    }
}
