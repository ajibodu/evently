using Evently.Core.Configurations;
using Evently.Core.Context;
using Evently.Core.HostedServices;
using Microsoft.Extensions.DependencyInjection;

namespace Evently.Core;

public static class ServiceRegistration
{
    public static IServiceCollection AddEvently(this IServiceCollection services, Action<IServiceCollection, Providers> options)
    {
        services.AddEventlyContext();
        options(services, new Providers(services));
        
        
        return services;
    }
}