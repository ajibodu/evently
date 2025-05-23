using Evently.Core.Configurations;

namespace Evently.Redis.Configurations;

public class RedisConsumerConfiguration: IBaseConsumerConfiguration
{
    public string? PreferredName { get; set; }
    public int? PreFetchCount { get; set; }
    public TimeSpan? PollingInterval { get; set; }
    public bool CreateIfNotExist { get; set; }
    public string EventName { get; set; }
    public Type EventType { get; set; }
    public Type ConsumerType { get; set; }
    public string ConsumerName { get; set; }
    public RetryConfiguration? RetryConfiguration { get; set; }
}