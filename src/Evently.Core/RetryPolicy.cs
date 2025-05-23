using Evently.Core.Configurations;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using Polly.Wrap;

namespace Evently.Core;

public class RetryPolicy
{
    public static AsyncPolicy Create(IBaseConsumerConfiguration configuration, BaseBusConfiguration config, ILogger logger)
    {
        var retryConfig = configuration.RetryConfiguration ?? config.RetryConfiguration;
        if (retryConfig == null)
            return Policy.NoOpAsync();
        
        var retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryAsync(
                retryConfig.Count,
                (attempt) => CalculateRetryDelay(attempt, retryConfig),
                onRetry: (ex, delay, attempt, context) =>
                {
                    context.TryGetValue("Identifier", value: out var identifier);
                    
                    logger.LogWarning(ex,
                        "{id} Retry attempt {Attempt}/{MaxAttempts} for {ConsumerName}. Next retry in {Delay}ms",
                        identifier, attempt, retryConfig.Count,
                        configuration.ConsumerName, delay.TotalMilliseconds);
                });

        var timeoutPolicy = Policy.TimeoutAsync(retryConfig.Timeout, TimeoutStrategy.Optimistic);
        var globalTimeout = Policy.TimeoutAsync(TimeSpan.FromMinutes(5), TimeoutStrategy.Optimistic);
        
        return Policy.WrapAsync(globalTimeout, retryPolicy, timeoutPolicy);
    }
    
    private static TimeSpan CalculateRetryDelay(int attempt, RetryConfiguration config)
    {
        var baseDelayMs = (config.Interval).TotalMilliseconds;

        return config.Strategy switch
        {
            RetryStrategy.FixedInterval => TimeSpan.FromMilliseconds(baseDelayMs),
            RetryStrategy.Incremental => TimeSpan.FromMilliseconds(baseDelayMs * attempt),
            RetryStrategy.ExponentialWithJitter => ExponentialBackoffWithJitter(baseDelayMs, attempt),
            _ => TimeSpan.FromMilliseconds(baseDelayMs)
        };
    }
    
    private static TimeSpan ExponentialBackoffWithJitter(double baseDelayMs, int attempt)
    {
        var maxJitter = baseDelayMs * 0.2;
        var jitter = new Random().NextDouble() * maxJitter;
        var delay = Math.Pow(2, attempt) * baseDelayMs + jitter;

        return TimeSpan.FromMilliseconds(delay);
    }
}