using System.Net;
using Confluent.Kafka;
using Evently.Core;
using Evently.Core.Configurations;
using Evently.Core.Context;
using Evently.Core.HostedServices;
using Evently.Kafka.HostedServices;
using Microsoft.Extensions.DependencyInjection;

namespace Evently.Kafka;

public static class ServiceRegistration
{
    public static IServiceCollection WithKafka(this Providers providers, IServiceCollection services, Action<Configurations.Configuration> config)
    {
        var kafkaConfig = new Configurations.Configuration(services.GetEventlyContext());
        config(kafkaConfig);
        ArgumentNullException.ThrowIfNull(kafkaConfig.BootStrapServers);

        services.AddSingleton<IKafkaMessenger, KafkaMessenger>();
        
        services.AddSingleton<Configurations.Configuration>(_ => kafkaConfig);
            
        foreach (var registration in kafkaConfig.KafkaConsumerConfigurations)
            services.AddScoped(typeof(IConsumer<>).MakeGenericType(registration.EventType), registration.ConsumerType);

        foreach (var topicProducer in kafkaConfig.TopicProducers)
            services.AddSingleton(topicProducer);
        
        if (kafkaConfig.KafkaConsumerConfigurations.Count > 0)
        {
            services.AddSingleton<IKafkaTopicManager, KafkaTopicManager>();
        
            // Register hosted service to initialize
            services.AddHostedService<Initializer>();
            services.AddHostedService<ConsumerService>();
        }
        
        return services;
    }
}

