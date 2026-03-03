using MediatR;

namespace MyCompany.Transfers.Application.Reports.Transfers;

public sealed record GetTransfersByPeriodReportQuery(
    TransfersCommonReportFilter Filter,
    TransfersReportGroupBy GroupBy,
    int Page = 1,
    int PageSize = 50)
    : IRequest<TransfersReportResult<TransfersByPeriodReportItemDto>>;

public sealed record GetTransfersByAgentReportQuery(
    TransfersCommonReportFilter Filter,
    int Page = 1,
    int PageSize = 50)
    : IRequest<TransfersReportResult<TransfersByAgentReportItemDto>>;

public sealed record GetTransfersByProviderReportQuery(
    TransfersCommonReportFilter Filter,
    int Page = 1,
    int PageSize = 50)
    : IRequest<TransfersReportResult<TransfersByProviderReportItemDto>>;

public sealed record GetTransfersRevenueReportQuery(
    TransfersCommonReportFilter Filter,
    TransfersReportGroupBy GroupBy,
    int Page = 1,
    int PageSize = 50)
    : IRequest<TransfersReportResult<TransfersRevenueReportItemDto>>;

