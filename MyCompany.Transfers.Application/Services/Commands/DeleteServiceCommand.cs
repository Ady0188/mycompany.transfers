using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Services.Commands;

public sealed record DeleteServiceCommand(string Id) : IRequest<ErrorOr<Success>>;

public sealed class DeleteServiceCommandHandler : IRequestHandler<DeleteServiceCommand, ErrorOr<Success>>
{
    private readonly IServiceRepository _services;
    private readonly IAccessRepository _access;
    private readonly IUnitOfWork _uow;

    public DeleteServiceCommandHandler(IServiceRepository services, IAccessRepository access, IUnitOfWork uow)
    {
        _services = services;
        _access = access;
        _uow = uow;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteServiceCommand cmd, CancellationToken ct)
    {
        var service = await _services.GetForUpdateAsync(cmd.Id, ct);
        if (service is null)
            return AppErrors.Common.NotFound($"Услуга '{cmd.Id}' не найдена.");
        if (await _access.AnyByServiceIdAsync(cmd.Id, ct))
            return AppErrors.Common.Validation("Невозможно удалить услугу: существуют привязанные права доступа агентов.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _services.Remove(service);
            return Task.FromResult(true);
        }, ct);

        return Result.Success;
    }
}
