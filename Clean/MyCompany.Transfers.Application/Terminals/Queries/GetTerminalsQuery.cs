using MediatR;
using MyCompany.Transfers.Application.Terminals.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Terminals.Queries;

public sealed record GetTerminalsQuery() : IRequest<IReadOnlyList<TerminalAdminDto>>;

public sealed class GetTerminalsQueryHandler : IRequestHandler<GetTerminalsQuery, IReadOnlyList<TerminalAdminDto>>
{
    private readonly ITerminalRepository _terminals;

    public GetTerminalsQueryHandler(ITerminalRepository terminals) => _terminals = terminals;

    public async Task<IReadOnlyList<TerminalAdminDto>> Handle(GetTerminalsQuery request, CancellationToken ct)
    {
        var list = await _terminals.GetAllAsync(ct);
        return list.Select(TerminalAdminDto.FromDomain).ToList();
    }
}
