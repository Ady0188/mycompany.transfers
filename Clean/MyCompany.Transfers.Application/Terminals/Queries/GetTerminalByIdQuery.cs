using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Terminals.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Terminals.Queries;

public sealed record GetTerminalByIdQuery(string TerminalId) : IRequest<ErrorOr<TerminalAdminDto>>;

public sealed class GetTerminalByIdQueryHandler : IRequestHandler<GetTerminalByIdQuery, ErrorOr<TerminalAdminDto>>
{
    private readonly ITerminalRepository _terminals;

    public GetTerminalByIdQueryHandler(ITerminalRepository terminals) => _terminals = terminals;

    public async Task<ErrorOr<TerminalAdminDto>> Handle(GetTerminalByIdQuery request, CancellationToken ct)
    {
        var terminal = await _terminals.GetAsync(request.TerminalId, ct);
        if (terminal is null)
            return AppErrors.Common.NotFound($"Терминал '{request.TerminalId}' не найден.");
        return TerminalAdminDto.FromDomain(terminal);
    }
}
