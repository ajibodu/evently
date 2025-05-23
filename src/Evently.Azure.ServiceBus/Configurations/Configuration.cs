using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Evently.Core.Configurations;
using Evently.Core.Context;

namespace Evently.Azure.ServiceBus.Configurations;

public class Configuration(IEventlyContext context) : BaseBusConfiguration
{
    public override IEventlyContext Context { get; } = context;
    
    private readonly ConcurrentDictionary<string, IReadOnlyList<ServiceBusConsumerConfiguration>> _cachedConfiguration = new();
    internal IReadOnlyList<ServiceBusConsumerConfiguration> ServiceBusConsumerConfigurations => _cachedConfiguration.GetOrAdd(nameof(ServiceBusConsumerConfiguration), _ => PrepareConfigurations());
    public override IReadOnlyList<IBaseConsumerConfiguration> GetConsumerConfigurations() => ServiceBusConsumerConfigurations;
    public override RetryConfiguration? RetryConfiguration { get; set; }
    internal IList<MessageProducer> EventProducers { get; set; } = [];
    public string ConnectionString { get; set; }
    public ServiceBusProcessorOptions ServiceBusProcessorOptions  { get; set; }
    public bool CreateTopicOrQueueIfNotExist { get; set; }
    private IReadOnlyList<ServiceBusConsumerConfiguration> PrepareConfigurations()
    {
        var busConfigMap = new Dictionary<(Type, Type), ServiceBusConsumerConfiguration>();
        if (context.TryGetBrokerConfiguration<ServiceBusConsumerConfiguration>(out var busConfig))
        {
            busConfigMap = busConfig
                .ToDictionary(
                    k => (k.EventType, k.ConsumerType),
                    k => new ServiceBusConsumerConfiguration{
                        TopicOrQueueName = k.TopicOrQueueName,
                        SubscriptionName = k.SubscriptionName,
                        CreateIfNotExist = k.CreateIfNotExist,
                        EventName = k.EventName,
                        EventType = k.EventType,
                        ConsumerType = k.ConsumerType,
                        ConsumerName = k.ConsumerName,
                        RuleOptions = k.RuleOptions,
                        ServiceBusProcessorOptions = k.ServiceBusProcessorOptions,
                        RetryConfiguration = k.RetryConfiguration
                    });
        }
        
        var finalConfigs = busConfigMap.Select(kv => kv.Value).ToList();

        if (context.TryGetBrokerConfiguration<BaseConsumerConfiguration>(out var consumerConfig))
        {
            var consumerConfigMap = consumerConfig
                .ToDictionary(c => (c.EventType, c.ConsumerType), c => c);
            
            foreach (var kv in consumerConfigMap)
            {
                if (!busConfigMap.ContainsKey(kv.Key))
                {
                    finalConfigs.Add(new ServiceBusConsumerConfiguration{
                        TopicOrQueueName = null,
                        SubscriptionName = null,
                        EventName = kv.Value.EventName,
                        EventType = kv.Value.EventType,
                        ConsumerType = kv.Value.ConsumerType,
                        ConsumerName = kv.Value.ConsumerName,
                        RuleOptions = null,
                        ServiceBusProcessorOptions = ServiceBusProcessorOptions,
                        RetryConfiguration = kv.Value.RetryConfiguration
                    });
                }
            }
        }
        
        return finalConfigs;
    }
}