using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Parameters.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Parameters.Commands;

public sealed record CreateParameterCommand(
    string? Id,
    string Code,
    string? Name,
    string? Description,
    string? Regex,
    bool Active = true) : IRequest<ErrorOr<ParameterAdminDto>>;

public sealed class CreateParameterCommandHandler : IRequestHandler<CreateParameterCommand, ErrorOr<ParameterAdminDto>>
{
    private readonly IParameterRepository _parameters;
    private readonly IUnitOfWork _uow;

    public CreateParameterCommandHandler(IParameterRepository parameters, IUnitOfWork uow)
    {
        _parameters = parameters;
        _uow = uow;
    }

    public async Task<ErrorOr<ParameterAdminDto>> Handle(CreateParameterCommand cmd, CancellationToken ct)
    {
        var id = string.IsNullOrWhiteSpace(cmd.Id)
            ? await _parameters.GetNextNumericIdAsync(ct)
            : cmd.Id.Trim();
        if (await _parameters.ExistsAsync(id, ct))
            return AppErrors.Common.Validation($"Параметр с Id '{id}' уже существует.");

        var param = ParamDefinition.Create(id, cmd.Code, cmd.Name, cmd.Description, cmd.Regex, cmd.Active);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _parameters.Add(param);
            return Task.FromResult(true);
        }, ct);

        return ParameterAdminDto.FromDomain(param);
    }
}
