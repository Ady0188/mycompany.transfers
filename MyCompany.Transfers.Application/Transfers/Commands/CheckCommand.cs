using ErrorOr;
using MediatR;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Transfers.Commands;

public sealed record CheckCommand(
    string AgentId,
    string ServiceId,
    TransferMethod Method,
    string Account) : IRequest<ErrorOr<CheckResponseDto>>;
