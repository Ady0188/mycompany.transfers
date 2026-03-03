using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Application.Transfers.Queries;

public sealed record GetTransferByExternalIdQuery(string AgentId, string ExternalId) : IRequest<ErrorOr<Transfer>>;

public sealed class GetTransferByExternalIdQueryHandler : IRequestHandler<GetTransferByExternalIdQuery, ErrorOr<Transfer>>
{
    private readonly ITransferReadRepository _transfers;

    public GetTransferByExternalIdQueryHandler(ITransferReadRepository transfers) => _transfers = transfers;

    public async Task<ErrorOr<Transfer>> Handle(GetTransferByExternalIdQuery request, CancellationToken ct)
    {
        var transfer = await _transfers.GetStatusByExternalIdAsync(request.AgentId, request.ExternalId, ct);
        if (transfer is null)
            return AppErrors.Transfers.NotFound(request.ExternalId);
        return transfer;
    }
}
