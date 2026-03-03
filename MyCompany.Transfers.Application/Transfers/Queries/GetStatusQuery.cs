using ErrorOr;
using MediatR;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Transfers.Queries;

public sealed record GetStatusQuery(string AgentId, string? ExternalId, string? TransferId)
    : IRequest<ErrorOr<StatusResponseDto>>;
