using MyCompany.Transfers.Admin.Client.Models;

namespace MyCompany.Transfers.Admin.Client.Services;

public interface ITransfersApiService
{
    Task<PagedResult<TransferAdminDto>> GetPagedAsync(int page = 1, int pageSize = 10, TransfersFilter? filter = null, CancellationToken ct = default);
}
