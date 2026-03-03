using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Parameters.Commands;

public sealed record DeleteParameterCommand(string Id) : IRequest<ErrorOr<Success>>;

public sealed class DeleteParameterCommandHandler : IRequestHandler<DeleteParameterCommand, ErrorOr<Success>>
{
    private readonly IParameterRepository _parameters;
    private readonly IUnitOfWork _uow;

    public DeleteParameterCommandHandler(IParameterRepository parameters, IUnitOfWork uow)
    {
        _parameters = parameters;
        _uow = uow;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteParameterCommand cmd, CancellationToken ct)
    {
        var param = await _parameters.GetForUpdateAsync(cmd.Id, ct);
        if (param is null)
            return AppErrors.Common.NotFound($"Параметр '{cmd.Id}' не найден.");
        if (await _parameters.AnyUsedByServiceAsync(cmd.Id, ct))
            return AppErrors.Common.Validation("Невозможно удалить параметр: он используется в одной или нескольких услугах.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _parameters.Remove(param);
            return Task.FromResult(true);
        }, ct);

        return Result.Success;
    }
}
