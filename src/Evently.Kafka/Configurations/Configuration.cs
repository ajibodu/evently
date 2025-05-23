using System.Collections.Concurrent;
using System.Net;
using Confluent.Kafka;
using Evently.Core.Configurations;
using Evently.Core.Context;

namespace Evently.Kafka.Configurations;

public class Configuration(IEventlyContext context) : BaseBusConfiguration
{
    public override IEventlyContext Context { get; } = context;
    private readonly ConcurrentDictionary<string, IReadOnlyList<KafkaConsumerConfiguration>> _cachedConfiguration = new();
    internal IReadOnlyList<KafkaConsumerConfiguration> KafkaConsumerConfigurations => _cachedConfiguration.GetOrAdd(nameof(KafkaConsumerConfiguration), _ => PrepareConfigurations());
    public override IReadOnlyList<IBaseConsumerConfiguration> GetConsumerConfigurations() => KafkaConsumerConfigurations;
    public override RetryConfiguration? RetryConfiguration { get; set; }
    internal IList<TopicProducer> TopicProducers { get; set; } = [];
    public string BootStrapServers { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    internal string GroupId { get; set; } = Dns.GetHostName();
    public ConsumerControl Control { get; set; } = null!;
    
    private IReadOnlyList<KafkaConsumerConfiguration> PrepareConfigurations()
    {
        var busConfigMap = new Dictionary<string, KafkaConsumerConfiguration>();
        if (context.TryGetBrokerConfiguration<KafkaConsumerConfiguration>(out var busConfig))
        {
            busConfigMap = busConfig
                .ToDictionary(
                    k => k.ConsumerName,
                    k => new KafkaConsumerConfiguration{
                        TopicName = k.TopicName ?? Collection.NameFormaterResolver().Format(k.EventType),
                        CreateTopicIfNotExist = k.CreateTopicIfNotExist,
                        EventName = k.EventName,
                        EventType = k.EventType,
                        ConsumerType = k.ConsumerType,
                        ConsumerName = k.ConsumerName,
                        RetryConfiguration = k.RetryConfiguration,
                        ConsumerConfig = k.ConsumerConfig,
                        CreateIfNotExistControl = k.CreateIfNotExistControl
                    });
        }
        
        var finalConfigs = busConfigMap.Select(kv => kv.Value).ToList();
        
        if(context.TryGetBrokerConfiguration<BaseConsumerConfiguration>(out var consumerConfig))
        {
            var consumerConfigMap = consumerConfig
                .ToDictionary(c => c.ConsumerName, c => c);
            
            foreach (var kv in consumerConfigMap)
            {
                if (!busConfigMap.ContainsKey(kv.Key))
                {
                    finalConfigs.Add(new KafkaConsumerConfiguration{
                        TopicName = Collection.NameFormaterResolver().Format(kv.Value.EventType),
                        EventName = kv.Value.EventName,
                        EventType = kv.Value.EventType,
                        ConsumerType = kv.Value.ConsumerType,
                        ConsumerName = kv.Value.ConsumerName,
                        RetryConfiguration = kv.Value.RetryConfiguration,
                        ConsumerConfig = new ConsumerConfig
                        {
                            BootstrapServers = this.BootStrapServers,
                            GroupId = this.GroupId,
                            AutoOffsetReset = AutoOffsetReset.Earliest,
                            SessionTimeoutMs = this.Control.SessionTimeoutMs,
                            RetryBackoffMs = this.Control.RetryBackoffMs,
                            EnableAutoCommit = this.Control.EnableAutoCommit,
                            PartitionAssignmentStrategy = PartitionAssignmentStrategy.RoundRobin
                        }
                    });
                }
            }
        }
        
        return finalConfigs;
    }
}