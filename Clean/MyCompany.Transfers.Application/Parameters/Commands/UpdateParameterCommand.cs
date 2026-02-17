using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Parameters.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Parameters.Commands;

public sealed record UpdateParameterCommand(
    string Id,
    string? Code,
    string? Name,
    string? Description,
    string? Regex,
    bool? Active) : IRequest<ErrorOr<ParameterAdminDto>>;

public sealed class UpdateParameterCommandHandler : IRequestHandler<UpdateParameterCommand, ErrorOr<ParameterAdminDto>>
{
    private readonly IParameterRepository _parameters;
    private readonly IUnitOfWork _uow;

    public UpdateParameterCommandHandler(IParameterRepository parameters, IUnitOfWork uow)
    {
        _parameters = parameters;
        _uow = uow;
    }

    public async Task<ErrorOr<ParameterAdminDto>> Handle(UpdateParameterCommand cmd, CancellationToken ct)
    {
        var param = await _parameters.GetForUpdateAsync(cmd.Id, ct);
        if (param is null)
            return AppErrors.Common.NotFound($"Параметр '{cmd.Id}' не найден.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            param.UpdateProfile(cmd.Code, cmd.Name, cmd.Description, cmd.Regex, cmd.Active);
            _parameters.Update(param);
            return Task.FromResult(true);
        }, ct);

        return ParameterAdminDto.FromDomain(param);
    }
}
