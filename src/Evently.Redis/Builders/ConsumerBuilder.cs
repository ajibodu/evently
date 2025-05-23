using Evently.Core;
using Evently.Core.Configurations;
using Evently.Core.Context;
using Evently.Redis.Configurations;

namespace Evently.Redis.Builders;

public class ConsumerBuilder(IEventlyContext context)
{
    public ConsumerBuilder AddConsumer<TEvent, TConsumer>(
        string? preferredName = null,
        int consumersPerStream = 1, 
        int prefetchCount = 10,
        TimeSpan? pollingInterval = null, 
        RetryConfiguration? retryConfiguration = null,
        bool createIfNotExist = false)
        where TEvent : new()
        where TConsumer : IConsumer<TEvent>
    {
        AddConfiguration(preferredName, consumersPerStream, prefetchCount, pollingInterval, retryConfiguration, typeof(TEvent), typeof(TConsumer), createIfNotExist);

        return this;
    }
    
    public ConsumerBuilder ConfigureConsumer<TConsumer>(
        string? preferredName = null,
        int consumersPerStream = 1, 
        int prefetchCount = 10,
        TimeSpan? pollingInterval = null, 
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
        
        AddConfiguration(preferredName, consumersPerStream, prefetchCount, pollingInterval, retryConfiguration, eventType, typeof(TConsumer), createIfNotExist);

        return this;
    }
    
    private void AddConfiguration(string? preferredName, int consumersPerStream, int? prefetchCount, TimeSpan? pollingInterval, RetryConfiguration? retryConfiguration, 
        Type eventType, Type consumerType, bool createIfNotExist)
    {
        for (var i = 0; i < consumersPerStream; i++)
        {
            context.RegisterConfiguration(new RedisConsumerConfiguration{
                PreferredName = preferredName,
                CreateIfNotExist = createIfNotExist,
                EventName = eventType.Name,
                EventType = eventType,
                ConsumerType = consumerType,
                ConsumerName = $"{consumerType.Name}-{i}",
                PreFetchCount = prefetchCount,
                PollingInterval = pollingInterval ?? TimeSpan.FromSeconds(1),
                RetryConfiguration = retryConfiguration
            });
        }
    }
}

