using Evently.Core.Context;

namespace Evently.Core.Configurations;

public abstract class BaseBusConfiguration
{
    public abstract IEventlyContext Context { get; }
    public abstract IReadOnlyList<IBaseConsumerConfiguration> GetConsumerConfigurations();
    public abstract RetryConfiguration? RetryConfiguration { get; set; } 
    public void WithRetry(RetryConfiguration retryConfiguration)
    {
        RetryConfiguration = retryConfiguration;
    }
}

public enum RetryStrategy
{
    FixedInterval,
    Incremental,
    ExponentialWithJitter
}

public class RetryConfiguration(RetryStrategy strategy, int count, TimeSpan interval, TimeSpan timeOut)
{
    public int Count { get; set; } = count;
    public TimeSpan Interval { get; set; } = interval;
    public RetryStrategy Strategy { get; set; } = strategy;
    public TimeSpan  Timeout { get; set; } = timeOut;
}