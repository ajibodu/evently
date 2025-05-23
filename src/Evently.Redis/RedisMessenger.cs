using Evently.Core;
using Evently.Core.Configurations;
using Evently.Core.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Evently.Redis;

public interface IRedisMessenger : IMessenger
{
    
}
public class RedisMessenger(
    IConnectionMultiplexer redis,
    Configurations.Configuration configuration,
    ILogger<RedisMessenger> logger) : IRedisMessenger
{
    private readonly JsonSerializerSettings _jsonSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        NullValueHandling = NullValueHandling.Ignore
    };

    public async Task SendAsync<T>(T message, CancellationToken cancellationToken)
    {
        var registration = configuration.RedisConsumerConfiguration.First(t => t.EventType == typeof(T));
        var db = redis.GetDatabase();
        
        await db.StreamAddAsync(registration.PreferredName, "Message", JsonConvert.SerializeObject(message, _jsonSettings)).ConfigureAwait(false);
        
        logger.LogDebug("Published message to {EventType}", registration.PreferredName);
    }
    
    public async Task SendAsync<T>(T message, Exception exception, CancellationToken cancellationToken)
    {
        var registration = configuration.RedisConsumerConfiguration.First(t => t.EventType == typeof(T));
        var db = redis.GetDatabase();
        
        await db.StreamAddAsync(registration.PreferredName!.ToDlQ(), [
            new NameValueEntry("Message", JsonConvert.SerializeObject(message, _jsonSettings)),
            new NameValueEntry("Ex-Type", exception.GetType().Name),
            new NameValueEntry("Ex-Message", exception.Message),
            new NameValueEntry("Ex-StackTrace", exception.StackTrace)
        ]).ConfigureAwait(false);
        
        logger.LogDebug("Published DLQ message for {StreamName}", registration.PreferredName);
    }
    
    public async Task ConsumeAsync<T>(string consumerName, Func<T, Task> messageHandler, CancellationToken cancellationToken)
    {
        var registration = configuration.RedisConsumerConfiguration.First(t => t.EventType == typeof(T));
        
        var db = redis.GetDatabase();
        while (!cancellationToken.IsCancellationRequested)
        {
            var entries = await db.StreamReadGroupAsync(registration.PreferredName, configuration.ConsumerGroup, consumerName, ">", count: registration.PreFetchCount, noAck: false);
            if (entries.Length > 0)
            {
                foreach (var entry in entries)
                {
                    var message = JsonConvert.DeserializeObject<T>(entry["Message"].ToString(), _jsonSettings);
                    await messageHandler(message);
                    await db.StreamAcknowledgeAsync(typeof(T).FullName, configuration.ConsumerGroup, entry.Id).ConfigureAwait(false);
                    await db.StreamDeleteAsync(typeof(T).FullName, [entry.Id]).ConfigureAwait(false);
                }
            }
            else
            {
                // No message available, wait for next
                await Task.Delay(registration.PollingInterval ?? TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        
        throw new OperationCanceledException();
    }

    public void Dispose()
    {
        redis?.Dispose();
    }
}