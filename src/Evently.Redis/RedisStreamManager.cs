using Evently.Core.Configurations;
using Evently.Redis.Configurations;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Evently.Redis;

public interface IRedisStreamManager
{
    Task RegisterQueuesAsync(IReadOnlyList<RedisConsumerConfiguration> consumerRegistrations);
}

public class RedisStreamManager(
    IConnectionMultiplexer redis,
    ILogger<RedisStreamManager> logger,
    Configuration streamConfig)
    : IRedisStreamManager
{
    public async Task RegisterQueuesAsync(IReadOnlyList<RedisConsumerConfiguration> consumerRegistrations)
    {
        var db = redis.GetDatabase();
        
        foreach (var registration in consumerRegistrations)
        {
            try
            {
                await db.StreamCreateConsumerGroupAsync(registration.PreferredName, streamConfig.ConsumerGroup, StreamPosition.NewMessages, createStream: true);
                
                logger.LogInformation("Created consumer group {Group} for queue {Queue}", streamConfig.ConsumerGroup, registration.EventName);
            }
            catch (RedisException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                logger.LogDebug("Consumer group {Group} already exists for queue {Queue}", streamConfig.ConsumerGroup, registration.EventName);
            }
            catch (RedisException ex) when (ex.Message.Contains("requires the key to exist"))
            {
                // Fallback for Redis < 6.2
                await db.StreamAddAsync(registration.PreferredName, "init", "init", maxLength: 0);
                await RegisterQueuesAsync([registration]);
            }
        }
    }
}