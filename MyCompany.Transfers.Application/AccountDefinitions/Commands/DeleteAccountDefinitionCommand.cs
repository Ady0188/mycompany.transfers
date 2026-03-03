using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.AccountDefinitions.Commands;

public sealed record DeleteAccountDefinitionCommand(Guid Id) : IRequest<ErrorOr<Success>>;

public sealed class DeleteAccountDefinitionCommandHandler : IRequestHandler<DeleteAccountDefinitionCommand, ErrorOr<Success>>
{
    private readonly IAccountDefinitionRepository _repo;
    private readonly IServiceRepository _services;
    private readonly IUnitOfWork _uow;

    public DeleteAccountDefinitionCommandHandler(IAccountDefinitionRepository repo, IServiceRepository services, IUnitOfWork uow)
    {
        _repo = repo;
        _services = services;
        _uow = uow;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteAccountDefinitionCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetForUpdateAsync(cmd.Id, ct);
        if (entity is null)
            return AppErrors.Common.NotFound($"Определение счёта с Id '{cmd.Id}' не найдено.");
        if (await _services.AnyByAccountDefinitionIdAsync(cmd.Id, ct))
            return AppErrors.Common.Validation("Невозможно удалить определение счёта: оно используется в одной или нескольких услугах.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _repo.Remove(entity);
            return Task.FromResult(true);
        }, ct);

        return Result.Success;
    }
}
