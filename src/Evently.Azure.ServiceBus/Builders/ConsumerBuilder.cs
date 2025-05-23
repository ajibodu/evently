using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Evently.Azure.ServiceBus.Configurations;
using Evently.Core;
using Evently.Core.Configurations;
using Evently.Core.Context;

namespace Evently.Azure.ServiceBus.Builders;

public class ConsumerBuilder(IEventlyContext context)
{
    public ConsumerBuilder AddQueueConsumer<TEvent, TConsumer>(
        string? queueName = null,
        int consumerPerQueue = 1, 
        ServiceBusProcessorOptions? processorOptions = null,
        RetryConfiguration? retryConfiguration = null,
        bool createIfNotExist = false)
        where TEvent : new()
        where TConsumer : IConsumer<TEvent>
    {
        AddConfiguration(queueName, null, consumerPerQueue, null, processorOptions, retryConfiguration, typeof(TEvent), typeof(TConsumer), createIfNotExist);

        return this;
    }
    
    public ConsumerBuilder ConfigureQueueConsumer<TConsumer>(
        string? queueName = null,
        int consumerPerQueue = 1, 
        ServiceBusProcessorOptions? processorOptions = null,
        RetryConfiguration? retryConfiguration = null,
        bool createIfNotExist = false)
        where TConsumer : class
    {
        // Find the IConsumer<TEvent> interface implementation
        var consumerInterface = typeof(TConsumer).GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>));
        
        if (consumerInterface == null)
            throw new ArgumentException($"{typeof(TConsumer).Name} must implement IConsumer<TEvent>");
        // Get the TEvent type
        var eventType = consumerInterface.GetGenericArguments()[0];
        
        AddConfiguration(queueName, null, consumerPerQueue, null, processorOptions, retryConfiguration, eventType, typeof(TConsumer), createIfNotExist);

        return this;
    }
    
    public ConsumerBuilder AddTopicConsumer<TEvent, TConsumer>(
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
        List<CreateRuleOptions>? ruleOptions = null;
        
        if (ruleOption != null)
        {
            var filterBuilder = new RuleOptionBuilder<TEvent>();
            ruleOption(filterBuilder);
            ruleOptions = filterBuilder.GetRules();
        }
        
        AddConfiguration(topicName, subscriptionName, consumerPerSubscription, ruleOptions, processorOptions, retryConfiguration, typeof(TEvent), typeof(TConsumer), createIfNotExist);

        return this;
    }
    
    private void AddConfiguration(string? topicOrQueueName, string? subscriptionName, int consumersPerStream, List<CreateRuleOptions>? ruleOptions, 
        ServiceBusProcessorOptions? processorOptions, RetryConfiguration? retryConfiguration, Type eventType, Type consumerType, bool createIfNotExist = false)
    {
        for (var i = 0; i < consumersPerStream; i++)
        {
            context.RegisterConfiguration(new ServiceBusConsumerConfiguration{
                TopicOrQueueName = topicOrQueueName,
                SubscriptionName = subscriptionName,
                CreateIfNotExist = createIfNotExist,
                EventName = eventType.Name,
                EventType = eventType,
                ConsumerType = consumerType,
                ConsumerName = $"{consumerType.Name}-{i}",
                RuleOptions = ruleOptions,
                ServiceBusProcessorOptions = processorOptions ?? new ServiceBusProcessorOptions(),
                RetryConfiguration = retryConfiguration
            });
        }
    }
}