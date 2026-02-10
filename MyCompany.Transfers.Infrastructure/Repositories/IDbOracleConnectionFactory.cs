using System.Data;

namespace MyCompany.Transfers.Infrastructure.Repositories;

internal interface IDbOracleConnectionFactory
{
    Task<IDbConnection> CreateOracleConnectionAsync(CancellationToken cancellationToken = default);
}
