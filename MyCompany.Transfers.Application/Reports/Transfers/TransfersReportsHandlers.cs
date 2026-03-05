using MediatR;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Reports.Transfers;

public sealed class GetTransfersByPeriodReportHandler
    : IRequestHandler<GetTransfersByPeriodReportQuery, TransfersReportResult<TransfersByPeriodReportItemDto>>
{
    private readonly ITransfersReportService _reports;

    public GetTransfersByPeriodReportHandler(ITransfersReportService reports) => _reports = reports;

    public Task<TransfersReportResult<TransfersByPeriodReportItemDto>> Handle(GetTransfersByPeriodReportQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = request.PageSize <= 0 ? TransfersReportLimits.DefaultPageSize : Math.Min(request.PageSize, TransfersReportLimits.MaxPageSize);
        return _reports.GetByPeriodAsync(request.Filter, request.GroupBy, page, size, ct);
    }
}

public sealed class GetTransfersByAgentReportHandler
    : IRequestHandler<GetTransfersByAgentReportQuery, TransfersReportResult<TransfersByAgentReportItemDto>>
{
    private readonly ITransfersReportService _reports;

    public GetTransfersByAgentReportHandler(ITransfersReportService reports) => _reports = reports;

    public Task<TransfersReportResult<TransfersByAgentReportItemDto>> Handle(GetTransfersByAgentReportQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = request.PageSize <= 0 ? TransfersReportLimits.DefaultPageSize : Math.Min(request.PageSize, TransfersReportLimits.MaxPageSize);
        return _reports.GetByAgentAsync(request.Filter, page, size, ct);
    }
}

public sealed class GetTransfersByProviderReportHandler
    : IRequestHandler<GetTransfersByProviderReportQuery, TransfersReportResult<TransfersByProviderReportItemDto>>
{
    private readonly ITransfersReportService _reports;

    public GetTransfersByProviderReportHandler(ITransfersReportService reports) => _reports = reports;

    public Task<TransfersReportResult<TransfersByProviderReportItemDto>> Handle(GetTransfersByProviderReportQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = request.PageSize <= 0 ? TransfersReportLimits.DefaultPageSize : Math.Min(request.PageSize, TransfersReportLimits.MaxPageSize);
        return _reports.GetByProviderAsync(request.Filter, page, size, ct);
    }
}

public sealed class GetTransfersRevenueReportHandler
    : IRequestHandler<GetTransfersRevenueReportQuery, TransfersReportResult<TransfersRevenueReportItemDto>>
{
    private readonly ITransfersReportService _reports;

    public GetTransfersRevenueReportHandler(ITransfersReportService reports) => _reports = reports;

    public Task<TransfersReportResult<TransfersRevenueReportItemDto>> Handle(GetTransfersRevenueReportQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = request.PageSize <= 0 ? TransfersReportLimits.DefaultPageSize : Math.Min(request.PageSize, TransfersReportLimits.MaxPageSize);
        return _reports.GetRevenueAsync(request.Filter, request.GroupBy, page, size, ct);
    }
}

public sealed class GetTransfersByBankReportHandler
    : IRequestHandler<GetTransfersByBankReportQuery, TransfersReportResult<TransfersByBankReportItemDto>>
{
    private readonly ITransfersReportService _reports;

    public GetTransfersByBankReportHandler(ITransfersReportService reports) => _reports = reports;

    public Task<TransfersReportResult<TransfersByBankReportItemDto>> Handle(GetTransfersByBankReportQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var size = request.PageSize <= 0 ? TransfersReportLimits.DefaultPageSize : Math.Min(request.PageSize, TransfersReportLimits.MaxPageSize);
        return _reports.GetByBankAsync(request.Filter, page, size, ct);
    }
}

