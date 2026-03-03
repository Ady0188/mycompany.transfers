using ErrorOr;
using MediatR;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Transfers.Commands;

public sealed record ConfirmCommand(string AgentId, string TerminalId, string ExternalId, string QuotationId)
    : IRequest<ErrorOr<ConfirmResponseDto>>;
