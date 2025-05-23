using System.Collections.Concurrent;
using System.Net;
using Evently.Core.Configurations;
using Evently.Core.Context;

namespace Evently.Redis.Configurations;

public class Configuration(IEventlyContext context) : BaseBusConfiguration
{
    public override IEventlyContext Context { get; } = context;
    private readonly ConcurrentDictionary<string, IReadOnlyList<RedisConsumerConfiguration>> _cachedConfiguration = new();
    internal IReadOnlyList<RedisConsumerConfiguration> RedisConsumerConfiguration  => _cachedConfiguration.GetOrAdd(nameof(RedisConsumerConfiguration), _ => PrepareConfigurations());
    public override IReadOnlyList<IBaseConsumerConfiguration> GetConsumerConfigurations() => RedisConsumerConfiguration;
    public override RetryConfiguration? RetryConfiguration { get; set; }
    public string ConnectionString { get; set; }
    internal string ConsumerGroup { get; set; } = Dns.GetHostName();
    
    private IReadOnlyList<RedisConsumerConfiguration> PrepareConfigurations()
    {
        var busConfigMap = new Dictionary<string, RedisConsumerConfiguration>();
        if (context.TryGetBrokerConfiguration<RedisConsumerConfiguration>(out var busConfig))
        {
            busConfigMap = busConfig
                .ToDictionary(
                    k => k.ConsumerName,
                    k => new RedisConsumerConfiguration{
                        PreferredName = k.PreferredName ?? Collection.NameFormaterResolver().Format(k.EventType),
                        CreateIfNotExist = k.CreateIfNotExist,
                        EventName = k.EventName,
                        EventType = k.EventType,
                        ConsumerType = k.ConsumerType,
                        ConsumerName = k.ConsumerName,
                        PollingInterval = k.PollingInterval,
                        RetryConfiguration = k.RetryConfiguration,
                        PreFetchCount = k.PreFetchCount,
                        
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
                    finalConfigs.Add(new RedisConsumerConfiguration{
                        PreferredName = Collection.NameFormaterResolver().Format(kv.Value.EventType),
                        EventName = kv.Value.EventName,
                        EventType = kv.Value.EventType,
                        ConsumerType = kv.Value.ConsumerType,
                        ConsumerName = kv.Value.ConsumerName,
                        PollingInterval = null,
                        RetryConfiguration = kv.Value.RetryConfiguration,
                        PreFetchCount = null
                    });
                }
            }
        }
        
        return finalConfigs;
    }
}