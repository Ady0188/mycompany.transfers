using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.AccountDefinitions.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Accounts;
using MyCompany.Transfers.Domain.Accounts.Enums;

namespace MyCompany.Transfers.Application.AccountDefinitions.Commands;

public sealed record UpdateAccountDefinitionCommand(
    Guid Id,
    string? Code,
    string? Regex,
    AccountNormalizeMode? Normalize,
    AccountAlgorithm? Algorithm,
    int? MinLength,
    int? MaxLength) : IRequest<ErrorOr<AccountDefinitionAdminDto>>;

public sealed class UpdateAccountDefinitionCommandHandler : IRequestHandler<UpdateAccountDefinitionCommand, ErrorOr<AccountDefinitionAdminDto>>
{
    private readonly IAccountDefinitionRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdateAccountDefinitionCommandHandler(IAccountDefinitionRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ErrorOr<AccountDefinitionAdminDto>> Handle(UpdateAccountDefinitionCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetForUpdateAsync(cmd.Id, ct);
        if (entity is null)
            return AppErrors.Common.NotFound($"Определение счёта с Id '{cmd.Id}' не найдено.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            entity.UpdateProfile(cmd.Code, cmd.Regex, cmd.Normalize, cmd.Algorithm, cmd.MinLength, cmd.MaxLength);
            _repo.Update(entity);
            return Task.FromResult(true);
        }, ct);

        return AccountDefinitionAdminDto.FromDomain(entity);
    }
}
