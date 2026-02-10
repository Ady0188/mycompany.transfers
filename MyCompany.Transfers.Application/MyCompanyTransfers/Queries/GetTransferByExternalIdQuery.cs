using ErrorOr;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Transfers;
using MediatR;

namespace MyCompany.Transfers.Application.MyCompanyTransfers.Queries;

public sealed record GetTransferByExternalIdQuery(string AgentId, string? ExternalId) : IRequest<ErrorOr<Transfer>>;
public class GetTransferByExternalIdQueryHandler : IRequestHandler<GetTransferByExternalIdQuery, ErrorOr<Transfer>>
{
    private readonly ITransferRepository _transferRepository;

    public GetTransferByExternalIdQueryHandler(ITransferRepository transferRepository)
    {
        _transferRepository = transferRepository;
    }

    public async Task<ErrorOr<Transfer>> Handle(GetTransferByExternalIdQuery request, CancellationToken cancellationToken)
    {
        var transfer = await _transferRepository.GetStatusByExternalIdAsync(request.AgentId, request.ExternalId, cancellationToken);

        if (transfer == null)
            return AppErrors.Transfers.NotFound(request.ExternalId);

        return transfer;
    }
}