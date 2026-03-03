using MyCompany.Transfers.Application.Reports.Transfers;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface ITransfersReportService
{
    Task<TransfersReportResult<TransfersByPeriodReportItemDto>> GetByPeriodAsync(
        TransfersCommonReportFilter filter,
        TransfersReportGroupBy groupBy,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<TransfersReportResult<TransfersByAgentReportItemDto>> GetByAgentAsync(
        TransfersCommonReportFilter filter,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<TransfersReportResult<TransfersByProviderReportItemDto>> GetByProviderAsync(
        TransfersCommonReportFilter filter,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<TransfersReportResult<TransfersRevenueReportItemDto>> GetRevenueAsync(
        TransfersCommonReportFilter filter,
        TransfersReportGroupBy groupBy,
        int page,
        int pageSize,
        CancellationToken ct);
}

