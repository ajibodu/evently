using Azure.Messaging.ServiceBus;
using Evently.Azure.ServiceBus.Builders;
using Evently.Azure.ServiceBus.Configurations;
using Evently.Core;
using Evently.Core.Configurations;

namespace Evently.Azure.ServiceBus.Extensions;

public static class ConfigurationExtensions
{
    public static ConsumerBuilder AddQueueConsumer<TEvent, TConsumer>(
        this Configuration configuration,
        string? queueName = null,
        int consumerPerQueue = 1, 
        ServiceBusProcessorOptions? processorOptions = null,
        RetryConfiguration? retryConfiguration = null,
        bool createIfNotExist = false)
        where TEvent : new()
        where TConsumer : IConsumer<TEvent>
    {
        var builder = new ConsumerBuilder(configuration.Context);
        builder.AddQueueConsumer<TEvent, TConsumer>(queueName, consumerPerQueue, processorOptions, retryConfiguration, createIfNotExist);

        return builder;
    }

    public static ConsumerBuilder AddTopicConsumer<TEvent, TConsumer>(
        this Configuration configuration,
        string? topicName = null,
        string? subscriptionName = null,
        int consumerPerSubscription = 1,
        ServiceBusProcessorOptions? processorOptions = null,
        Action<RuleOptionBuilder<TEvent>>? ruleOption = null,
        RetryConfiguration? retryConfiguration = null,
        bool createIfNotExist = false)
        where TEvent : new()
        where TConsumer : IConsumer<TEvent>
    {
        var builder = new ConsumerBuilder(configuration.Context);
        builder.AddTopicConsumer<TEvent, TConsumer>(topicName, subscriptionName, consumerPerSubscription, processorOptions, ruleOption, retryConfiguration, createIfNotExist);

        return builder;
    }
    
    public static ProducerBuilder WithProducers(this Configuration configuration)
    {
        var serviceBusClient = new ServiceBusClient(configuration.ConnectionString);
        return new ProducerBuilder(configuration, serviceBusClient);
    }
}