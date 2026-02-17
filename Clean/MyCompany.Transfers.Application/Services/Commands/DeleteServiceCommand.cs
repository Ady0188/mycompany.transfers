using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Services.Commands;

public sealed record DeleteServiceCommand(string Id) : IRequest<ErrorOr<Success>>;

public sealed class DeleteServiceCommandHandler : IRequestHandler<DeleteServiceCommand, ErrorOr<Success>>
{
    private readonly IServiceRepository _services;
    private readonly IUnitOfWork _uow;

    public DeleteServiceCommandHandler(IServiceRepository services, IUnitOfWork uow)
    {
        _services = services;
        _uow = uow;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteServiceCommand cmd, CancellationToken ct)
    {
        var service = await _services.GetForUpdateAsync(cmd.Id, ct);
        if (service is null)
            return AppErrors.Common.NotFound($"Услуга '{cmd.Id}' не найдена.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _services.Remove(service);
            return Task.FromResult(true);
        }, ct);

        return Result.Success;
    }
}
