namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task CommitChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteTransactionalAsync(Func<CancellationToken, Task<bool>> work, CancellationToken ct = default);
}
