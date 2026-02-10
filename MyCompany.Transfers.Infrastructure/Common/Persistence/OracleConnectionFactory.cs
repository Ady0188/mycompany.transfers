using MyCompany.Transfers.Infrastructure.Repositories;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace MyCompany.Transfers.Infrastructure.Common.Persistence;

internal class OracleConnectionFactory(string connectionString) : IDbOracleConnectionFactory
{
    private readonly string _connectionString = connectionString;

    public async Task<IDbConnection> CreateOracleConnectionAsync(CancellationToken cancellationToken = default)
    {
        OracleConnection connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
