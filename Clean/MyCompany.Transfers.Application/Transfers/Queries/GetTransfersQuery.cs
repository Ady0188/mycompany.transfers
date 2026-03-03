using MediatR;
using MyCompany.Transfers.Application.Common.Models;
using MyCompany.Transfers.Application.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Transfers.Queries;

public sealed record GetTransfersQuery(int Page = 1, int PageSize = 10, TransfersFilter? Filter = null)
    : IRequest<PagedResult<TransferAdminDto>>;
