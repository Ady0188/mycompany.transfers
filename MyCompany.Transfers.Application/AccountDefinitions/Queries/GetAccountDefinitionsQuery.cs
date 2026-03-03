using MediatR;
using MyCompany.Transfers.Application.AccountDefinitions.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.AccountDefinitions.Queries;

public sealed record GetAccountDefinitionsQuery() : IRequest<IReadOnlyList<AccountDefinitionAdminDto>>;

public sealed class GetAccountDefinitionsQueryHandler : IRequestHandler<GetAccountDefinitionsQuery, IReadOnlyList<AccountDefinitionAdminDto>>
{
    private readonly IAccountDefinitionRepository _repo;

    public GetAccountDefinitionsQueryHandler(IAccountDefinitionRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<AccountDefinitionAdminDto>> Handle(GetAccountDefinitionsQuery request, CancellationToken ct)
    {
        var list = await _repo.GetAllAsync(ct);
        return list.Select(AccountDefinitionAdminDto.FromDomain).ToList();
    }
}
