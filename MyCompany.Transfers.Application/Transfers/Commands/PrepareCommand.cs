using ErrorOr;
using MediatR;
using MyCompany.Transfers.Contract.Core.Requests;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Transfers.Commands;

public sealed record PrepareCommand(
    string AgentId,
    string TerminalId,
    string ExternalId,
    TransferMethod Method,
    string Account,
    long Amount,
    string Currency,
    string? PayoutCurrency,
    string ServiceId,
    Dictionary<string, string> Parameters) : IRequest<ErrorOr<PrepareResponseDto>>;
