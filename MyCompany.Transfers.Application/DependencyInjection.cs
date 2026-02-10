using FluentValidation;
using MyCompany.Transfers.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Reflection;

namespace MyCompany.Transfers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<ICurrencyCatalog, DefaultCurrencyCatalog>();

        ConfigNLog();

        return services;
    }

    private static void ConfigNLog()
    {
        var config = new LoggingConfiguration();

        var fileTarget = new FileTarget("logfile")
        {
            FileName = "Logs/log.log",
            ArchiveFileName = "Logs/log_{#}.log",
            ArchiveEvery = FileArchivePeriod.Day,
            ArchiveNumbering = ArchiveNumberingMode.Rolling,
            MaxArchiveFiles = 365,
            EnableArchiveFileCompression = true, // архивы будут *.gz
            Layout = "${longdate} ${level:uppercase=true} ${logger} ${aspnet-traceidentifier} line ${callsite-linenumber} ${message}"
        };

        config.AddTarget(fileTarget);
        config.AddRuleForAllLevels(fileTarget);


        LogManager.Configuration = config;
    }
}
