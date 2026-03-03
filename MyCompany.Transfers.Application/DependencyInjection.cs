using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MyCompany.Transfers.Application.Common.Behaviors;
using System.Reflection;

namespace MyCompany.Transfers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
