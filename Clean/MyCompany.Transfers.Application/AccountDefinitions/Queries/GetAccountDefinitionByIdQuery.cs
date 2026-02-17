using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.AccountDefinitions.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Accounts;

namespace MyCompany.Transfers.Application.AccountDefinitions.Queries;

public sealed record GetAccountDefinitionByIdQuery(Guid Id) : IRequest<ErrorOr<AccountDefinitionAdminDto>>;

public sealed class GetAccountDefinitionByIdQueryHandler : IRequestHandler<GetAccountDefinitionByIdQuery, ErrorOr<AccountDefinitionAdminDto>>
{
    private readonly IAccountDefinitionRepository _repo;

    public GetAccountDefinitionByIdQueryHandler(IAccountDefinitionRepository repo) => _repo = repo;

    public async Task<ErrorOr<AccountDefinitionAdminDto>> Handle(GetAccountDefinitionByIdQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetAsync(request.Id, ct);
        if (entity is null)
            return AppErrors.Common.NotFound($"Определение счёта с Id '{request.Id}' не найдено.");
        return AccountDefinitionAdminDto.FromDomain(entity);
    }
}
