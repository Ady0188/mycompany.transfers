using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Infrastructure.Caching;
using MyCompany.Transfers.Infrastructure.Persistence;
using MyCompany.Transfers.Infrastructure.Providers;
using MyCompany.Transfers.Infrastructure.Repositories;
using MyCompany.Transfers.Infrastructure.Workers;
using Npgsql;

namespace MyCompany.Transfers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not set.");

        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.EnableDynamicJson();
        var dataSource = builder.Build();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(dataSource, npg => npg.EnableRetryOnFailure()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddMemoryCache(o => o.SizeLimit = 1024 * 1024 * 100);
        services.AddSingleton<ICacheService, MemoryCacheService>();

        services.AddScoped<ITransferReadRepository, TransferRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IOutboxReadRepository, OutboxRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IFxRateRepository, FxRateRepository>();
        services.AddScoped<ICurrencyConverter, CurrencyConverter>();

        services.AddScoped<AgentRepository>();
        services.AddScoped<IAgentReadRepository>(sp =>
            new CachedAgentReadRepository(sp.GetRequiredService<AgentRepository>(), sp.GetRequiredService<ICacheService>()));

        services.AddScoped<AccessRepository>();
        services.AddScoped<IAccessRepository>(sp =>
            new CachedAccessRepository(sp.GetRequiredService<AccessRepository>(), sp.GetRequiredService<ICacheService>()));

        services.AddScoped<ServiceRepository>();
        services.AddScoped<IServiceRepository>(sp =>
            new CachedServiceRepository(sp.GetRequiredService<ServiceRepository>(), sp.GetRequiredService<ICacheService>()));

        services.AddScoped<ProviderRepository>();
        services.AddScoped<IProviderRepository>(sp =>
            new CachedProviderRepository(sp.GetRequiredService<ProviderRepository>(), sp.GetRequiredService<ICacheService>()));

        services.AddScoped<TerminalRepository>();
        services.AddScoped<ITerminalRepository>(sp =>
            new CachedTerminalRepository(sp.GetRequiredService<TerminalRepository>(), sp.GetRequiredService<ICacheService>()));

        services.AddScoped<ParameterRepository>();
        services.AddScoped<IParameterRepository>(sp =>
            new CachedParameterRepository(sp.GetRequiredService<ParameterRepository>(), sp.GetRequiredService<ICacheService>()));

        services.AddScoped<AccountDefinitionRepository>();
        services.AddScoped<IAccountDefinitionRepository>(sp =>
            new CachedAccountDefinitionRepository(sp.GetRequiredService<AccountDefinitionRepository>(), sp.GetRequiredService<ICacheService>()));

        services.AddScoped<IProviderTokenService, ProviderTokenService>();
        services.AddSingleton<IProviderHttpHandlerCache, ProviderHttpHandlerCache>();

        services.AddSingleton<IProviderClient, OracleProviderClient>();
        services.AddSingleton<IProviderClient, PayPorterClient>();
        services.AddSingleton<IProviderClient, IPSClient>();
        services.AddSingleton<IProviderClient, FIMIClient>();
        services.AddSingleton<IProviderClient, SberClient>();
        services.AddSingleton<IProviderClient, TBankClient>();

        services.AddScoped<IProviderSender, HttpProviderSender>();
        services.AddScoped<IProviderService, ProviderService>();
        services.AddHttpClient("base");
        services.AddHttpClient("Sber").ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            return handler;
        });

        services.AddHostedService<ProviderSenderWorker>();

        return services;
    }
}
