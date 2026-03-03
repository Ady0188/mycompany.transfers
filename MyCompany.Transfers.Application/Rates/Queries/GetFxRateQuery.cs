using ErrorOr;
using MediatR;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Rates.Queries;

public sealed record GetFxRateQuery(string AgentId, string From, string To)
    : IRequest<ErrorOr<CurrencyDto>>;
