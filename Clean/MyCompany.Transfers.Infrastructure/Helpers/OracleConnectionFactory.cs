using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace MyCompany.Transfers.Infrastructure.Helpers;

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