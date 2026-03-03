using ErrorOr;
using MediatR;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Agents.Queries;

public sealed record GetBalanceQuery(string AgentId, string? Currency)
    : IRequest<ErrorOr<BalanceResponseDto>>;
