using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Evently.Core;
using Evently.Core.Configurations;
using Evently.Kafka.Configurations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Evently.Kafka;

public interface IKafkaMessenger : IMessenger
{
    Task SendAsync<T>(object key, T message, Headers? headers, CancellationToken cancellationToken);
}

public class KafkaMessenger(
    ILogger<KafkaMessenger> logger,
    Configuration configuration,
    IEnumerable<TopicProducer> producers) : IKafkaMessenger
{
    public async Task SendAsync<T>(T message, CancellationToken cancellationToken)
    {
        await SendAsync(null, message, [], cancellationToken);
    }
    
    public async Task SendAsync<T>(object key, T message, Headers? headers, CancellationToken cancellationToken)
    {
        var current = producers.First(t => t.EventType == typeof(T) && t.IsDlQ == false);
        await current.SendAsync(key, message, headers, cancellationToken);
        
        logger.LogDebug("Published message for {EventType}", current.EventType.FullName);
    }

    public async Task SendAsync<T>(T message, Exception exception, CancellationToken cancellationToken)
    {
        var current = producers.First(t => t.EventType == typeof(T) && t.IsDlQ == true);
        var headers = new Headers
        {
            { "Ex-Type", Encoding.UTF8.GetBytes(exception.GetType().Name) },
            { "Ex-Message", Encoding.UTF8.GetBytes(exception.Message) },
            { "Ex-StackTrace", Encoding.UTF8.GetBytes(exception.StackTrace ?? "") }
        };
        await current.SendAsync(null, message, headers, cancellationToken);
        
        logger.LogDebug("Published DLQ message for {EventType}", current.EventType.FullName);
    }

    public async Task ConsumeAsync<T>(string consumerName, Func<T, Task> messageHandler, CancellationToken cancellationToken)
    {
        var registration = configuration.KafkaConsumerConfigurations.First(t => t.EventType == typeof(T));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = new ConsumerBuilder<Ignore, T>(registration.ConsumerConfig)
                    .SetValueDeserializer(new NewtonsoftJsonDeserializer<T>())
                    .SetLogHandler((_, message) => KafkaLoggingHelper.LogKafkaMessage(message))
                    .SetErrorHandler((_, error) => KafkaLoggingHelper.LogKafkaError(error))
                    .Build();
        
                consumer.Subscribe(registration.TopicName);
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = new ConsumeResult<Ignore, T>();
                    try
                    { 
                        consumeResult = consumer.Consume(cancellationToken);
                        if (consumeResult != null && consumeResult.Message.Value != null)
                            await messageHandler(consumeResult.Message.Value);

                        if(registration.ConsumerConfig.EnableAutoCommit == false)
                            consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        logger.LogError(ex, "Kafka consume error");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Kafka consume error. attempting to dlq...");
                        var actualEx = ex is TargetInvocationException tie ? tie.InnerException : ex;
                        if (consumeResult != null && consumeResult.Message.Value != null)
                            await SendAsync(consumeResult.Message.Value, actualEx, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Cancellation requested");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled Kafka consumer error. Restarting...");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            
        }
        throw new OperationCanceledException();
    }
    
    private static LogLevel LogLevelFromKafka(SyslogLevel level)
    {
        return level switch
        {
            SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical => LogLevel.Critical,
            SyslogLevel.Error => LogLevel.Error,
            SyslogLevel.Warning => LogLevel.Warning,
            SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
            SyslogLevel.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };
    }

}