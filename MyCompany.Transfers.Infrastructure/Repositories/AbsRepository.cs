using Dapper;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers.Responses;
using Microsoft.EntityFrameworkCore;
using NLog;
using System.Data;
using System.Text.Json;

namespace MyCompany.Transfers.Infrastructure.Repositories;

internal class AbsRepository : IAbsRepository
{
    private readonly IDbOracleConnectionFactory _dbConnectionFactory;
    private Logger _logger = LogManager.GetCurrentClassLogger();

    public AbsRepository(IDbOracleConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<AbsCheckResponse?> CheckAbsAsync(string request, CancellationToken cancellationToken = default)
    {

        var result = await SendRequest(request, cancellationToken);

        return JsonSerializer.Deserialize<AbsCheckResponse?>(result);
    }

    private async Task<string> SendRequest(string request, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateOracleConnectionAsync(cancellationToken);
        var parameters = new DynamicParameters();
        parameters.Add("strin", request, DbType.String, ParameterDirection.Input);
        parameters.Add("errcode", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("clobResult", dbType: DbType.String, direction: ParameterDirection.Output, size: int.MaxValue);

        // Выполнение запроса с использованием Dapper
        await connection.ExecuteAsync("company_mobile_banking.run_synch_query", parameters, commandType: CommandType.StoredProcedure);

        int errcode = parameters.Get<int>("errcode");
        string response = parameters.Get<string>("clobResult");

        response = response.Replace("<?xml version=\"1.0\"?>", "").Replace("OK_UTG", "OK").Trim().Replace("\n", "").Replace("\r", "");

        if (string.IsNullOrWhiteSpace(response))
            return null;

        _logger.Debug($"Response ABS check client: {response} {request}");

        return response;
    }
}
