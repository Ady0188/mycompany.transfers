using System.Data;

namespace MyCompany.Transfers.Infrastructure.Helpers;

internal interface IDbOracleConnectionFactory
{
    Task<IDbConnection> CreateOracleConnectionAsync(CancellationToken cancellationToken = default);
}