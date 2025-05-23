using Evently.Core.Configurations;
using Evently.Core.Context;

namespace Evently.Core.Builders;

public class ConsumerBuilder(IEventlyContext eventlyContext)
{
    public ConsumerBuilder AddConsumer<TEvent, TConsumer>(
        int consumersPerStream = 1, 
        RetryConfiguration? retryConfiguration = null)
        where TEvent : new()
        where TConsumer : IConsumer<TEvent>
    {
        
        for (var i = 0; i < consumersPerStream; i++)
        {
            eventlyContext.RegisterConfiguration(new BaseConsumerConfiguration{
                EventName = typeof(TEvent).Name,
                EventType = typeof(TEvent),
                ConsumerType = typeof(TConsumer),
                ConsumerName = $"{typeof(TConsumer).Name}-{i}",
                RetryConfiguration = retryConfiguration
            });
        }

        return this;
    }
}