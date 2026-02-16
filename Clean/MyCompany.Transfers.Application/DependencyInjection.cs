using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MyCompany.Transfers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
