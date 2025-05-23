using Azure.Messaging.ServiceBus;
using Evently.Azure.ServiceBus.HostedServices;
using Evently.Core;
using Evently.Core.Configurations;
using Evently.Core.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Evently.Azure.ServiceBus;

public static class ServiceRegistration
{
    public static IServiceCollection WithAzureServiceBus(this Providers providers, IServiceCollection services, Action<Configurations.Configuration> config)
    {
        var serviceBusConfig = new Configurations.Configuration(services.GetEventlyContext());
        config(serviceBusConfig);
        ArgumentNullException.ThrowIfNull(serviceBusConfig.ConnectionString);

        services.AddSingleton<IServiceBusMessenger, ServiceBusMessenger>();
        
        services.AddSingleton<Configurations.Configuration>(_ => serviceBusConfig);
        
        services.AddSingleton<ServiceBusClient>(sp => new ServiceBusClient(serviceBusConfig.ConnectionString));
        
        foreach (var eventProducer in serviceBusConfig.EventProducers)
            services.AddSingleton(eventProducer);
        
        foreach (var registration in serviceBusConfig.ServiceBusConsumerConfigurations)
            services.AddScoped(typeof(IConsumer<>).MakeGenericType(registration.EventType), registration.ConsumerType);
        
        if (serviceBusConfig.ServiceBusConsumerConfigurations.Count > 0)
        {
            services.AddSingleton<IServiceBusQueueAndTopicManager, ServiceBusQueueAndTopicManager>();
        
            // Register hosted service to initialize streams
            services.AddHostedService<Initializer>();
            services.AddHostedService<ConsumerService>();
        }
        
        return services;
    }
}