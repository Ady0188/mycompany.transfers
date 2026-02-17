using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.AccountDefinitions.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Accounts;
using MyCompany.Transfers.Domain.Accounts.Enums;

namespace MyCompany.Transfers.Application.AccountDefinitions.Commands;

public sealed record CreateAccountDefinitionCommand(
    Guid? Id,
    string Code,
    string? Regex,
    AccountNormalizeMode Normalize,
    AccountAlgorithm Algorithm,
    int? MinLength,
    int? MaxLength) : IRequest<ErrorOr<AccountDefinitionAdminDto>>;

public sealed class CreateAccountDefinitionCommandHandler : IRequestHandler<CreateAccountDefinitionCommand, ErrorOr<AccountDefinitionAdminDto>>
{
    private readonly IAccountDefinitionRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateAccountDefinitionCommandHandler(IAccountDefinitionRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ErrorOr<AccountDefinitionAdminDto>> Handle(CreateAccountDefinitionCommand cmd, CancellationToken ct)
    {
        if (cmd.Id.HasValue && await _repo.ExistsAsync(cmd.Id.Value, ct))
            return AppErrors.Common.Validation($"Определение счёта с Id '{cmd.Id}' уже существует.");

        var entity = cmd.Id.HasValue
            ? AccountDefinition.CreateWithId(cmd.Id.Value, cmd.Code, cmd.Regex, cmd.Normalize, cmd.Algorithm, cmd.MinLength, cmd.MaxLength)
            : AccountDefinition.Create(cmd.Code, cmd.Regex, cmd.Normalize, cmd.Algorithm, cmd.MinLength, cmd.MaxLength);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _repo.Add(entity);
            return Task.FromResult(true);
        }, ct);

        return AccountDefinitionAdminDto.FromDomain(entity);
    }
}
