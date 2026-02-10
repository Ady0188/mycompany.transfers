using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Infrastructure.Common.Caching;
using MyCompany.Transfers.Infrastructure.Common.Persistence;
using MyCompany.Transfers.Infrastructure.Providers;
using MyCompany.Transfers.Infrastructure.Repositories;
using MyCompany.Transfers.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Security.Cryptography.X509Certificates;

namespace MyCompany.Transfers.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var npgsqlBuilder = new NpgsqlDataSourceBuilder(configuration["ConnectionStrings:DefaultConnection"]);
        npgsqlBuilder.EnableDynamicJson();           // <-- ВАЖНО
                                                     // если используете Newtonsoft.Json вместо System.Text.Json:
                                                     // npgsqlBuilder.UseJsonNet();

        services.AddSingleton<IDbOracleConnectionFactory>(serviceProvider =>
            new OracleConnectionFactory(serviceProvider.GetRequiredService<IConfiguration>()["ConnectionStrings:OracleConnection"]!));

        var dataSource = npgsqlBuilder.Build();

        // 2) Подсовываем data source EF Core
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(dataSource, npg => npg.EnableRetryOnFailure()).EnableSensitiveDataLogging()
      .LogTo(Console.WriteLine, LogLevel.Information));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());

        //services.BuildServiceProvider().GetService<AppDbContext>()!.Database.Migrate();

        services.AddSingleton<IProviderHttpHandlerCache, ProviderHttpHandlerCache>();
        services.AddSingleton<IProviderClient, OracleProviderClient>();
        services.AddSingleton<IProviderClient, PayProrterClient>();
        services.AddSingleton<IProviderClient, IPSClient>();
        services.AddSingleton<IProviderClient, FIMIClient>();
        services.AddSingleton<IProviderClient, SberClient>();
        services.AddSingleton<IProviderClient, TBankClient>();
        //services.AddSingleton<IProviderClient, FooPayClient>();
        //services.AddSingleton<IProviderRouter, ProviderRouter>();

        services.AddScoped<IProviderTokenService, ProviderTokenService>();
        services.AddScoped<IProviderSender, HttpProviderSender>();
        services.AddScoped<IProviderGateway, ProviderGateway>();
        services.AddScoped<IProviderService, ProviderService>();
        services.AddHttpClient("base");
        services.AddHttpClient("Sber")
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var path = "certificates\\sber\\requestcert.pfx";

                X509Certificate2 certificate = new X509Certificate2(path, "qwe123");

                // Create an HttpClientHandler and configure it to use the certificate
                HttpClientHandler handler = new HttpClientHandler();
                handler.ClientCertificates.Add(certificate);
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, SslPolicyErrors) => true;

                return handler;
            })
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/xml");
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(20));

        services.AddScoped<ITransferReadRepository, TransferRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IOutboxReadRepository, OutboxRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IFxRateRepository, FxRateRepository>();
        services.AddScoped<ICurrencyConverter, CurrencyConverter>();
        //services.AddScoped<IAgentReadRepository, AgentRepository>();
        //services.AddScoped<IAgentRepository, AgentRepository>();
        //services.AddScoped<IServiceRepository, ServiceRepository>();
        //services.AddScoped<IProviderRepository, ProviderRepository>();
        //services.AddScoped<ITerminalRepository, TerminalRepository>();
        //services.AddScoped<IAccessRepository, AccessRepository>();
        //services.AddScoped<IParameterRepository, ParameterRepository>();
        //services.AddScoped<IAccountDefinitionRepository, AccountDefinitionRepository>();
        services.AddScoped<IAbsRepository, AbsRepository>();

        services.AddMemoryCache(o =>
        {
            o.SizeLimit = 1024 * 1024 * 100; // ~100 MB
        });
        services.AddSingleton<ICacheService, MemoryCacheService>();

        services.AddScoped<AccountDefinitionRepository>();
        services.AddScoped<IAccountDefinitionRepository>(sp =>
            new CachedAccountDefinitionRepository(
                sp.GetRequiredService<AccountDefinitionRepository>(),
                sp.GetRequiredService<ICacheService>()
            ));

        services.AddScoped<ParameterRepository>();
        services.AddScoped<IParameterRepository>(sp =>
            new CachedParameterRepository(
                sp.GetRequiredService<ParameterRepository>(),
                sp.GetRequiredService<ICacheService>()
            ));

        services.AddScoped<ProviderRepository>();
        services.AddScoped<IProviderRepository>(sp =>
            new CachedProviderRepository(
                sp.GetRequiredService<ProviderRepository>(),
                sp.GetRequiredService<ICacheService>()
            ));

        services.AddScoped<ServiceRepository>();
        services.AddScoped<IServiceRepository>(sp =>
            new CachedServiceRepository(
                sp.GetRequiredService<ServiceRepository>(),
                sp.GetRequiredService<ICacheService>()
            ));

        services.AddScoped<AccessRepository>();
        services.AddScoped<IAccessRepository>(sp =>
            new CachedAccessRepository(
                sp.GetRequiredService<AccessRepository>(),
                sp.GetRequiredService<ICacheService>()
            ));

        services.AddScoped<TerminalRepository>();
        services.AddScoped<ITerminalRepository>(sp =>
            new CachedTerminalRepository(
                sp.GetRequiredService<TerminalRepository>(),
                sp.GetRequiredService<ICacheService>()
            ));

        services.AddScoped<AgentRepository>();
        services.AddScoped<IAgentRepository>(sp =>
            sp.GetRequiredService<AgentRepository>());

        services.AddScoped<IAgentReadRepository>(sp =>
            new CachedAgentReadRepository(
                sp.GetRequiredService<AgentRepository>(),
                sp.GetRequiredService<ICacheService>()));

        services.AddHostedService<ProviderSenderWorker>();

        return services;
    }
}
