using Azure.Messaging.ServiceBus;
using Evently.Azure.ServiceBus.Configurations;
using Evently.Core;
using Evently.Core.Configurations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Evently.Azure.ServiceBus;

public interface IServiceBusMessenger : IMessenger
{
    Task SendAsync<T>(T message, Dictionary<string, object> properties, CancellationToken cancellationToken);
}

public class ServiceBusMessenger(
    ILogger<ServiceBusMessenger> logger,
    Configurations.Configuration configuration,
    IEnumerable<MessageProducer> producers,
    ServiceBusClient serviceBusClient) : IServiceBusMessenger
{
    public async Task SendAsync<T>(T message, CancellationToken cancellationToken)
    {
        await SendAsync(message, new Dictionary<string, object>(), cancellationToken);
    }
    
    public async Task SendAsync<T>(T message, Dictionary<string, object> properties, CancellationToken cancellationToken)
    {
        var current = producers.First(t => t.EventType == typeof(T));
        await current.SendAsync(message, properties, cancellationToken);
        
        logger.LogDebug("Published message for {EventType}", current.EventType.FullName);
    }

    public async Task SendAsync<T>(T message, Exception exception, CancellationToken cancellationToken)
    {
        var current = producers.First(t => t.EventType == typeof(T));
        var headers = new Dictionary<string, object>
        {
            { "Ex-Type", exception.GetType().Name },
            { "Ex-Message", exception.Message },
            { "Ex-StackTrace", exception.StackTrace ?? string.Empty }
        };
        await current.SendAsync(message, headers, cancellationToken);
        
        logger.LogDebug("Published DLQ message for {EventType}", current.EventType.FullName);
    }

    public async Task ConsumeAsync<T>(string consumerName, Func<T, Task> messageHandler, CancellationToken cancellationToken)
    {
        var registration = configuration.ServiceBusConsumerConfigurations.First(t => t.EventType == typeof(T));
        
        var topicOrQueueName = registration.TopicOrQueueName ?? Collection.NameFormaterResolver().Format(registration.EventType);
        var processor = registration.SubscriptionName == null 
            ? serviceBusClient.CreateProcessor(topicOrQueueName, registration.ServiceBusProcessorOptions)
            : serviceBusClient.CreateProcessor(topicOrQueueName, registration.SubscriptionName, registration.ServiceBusProcessorOptions);
        
        processor.ProcessMessageAsync += async (args) =>
        {
            try
            {
                var json = args.Message.Body.ToString();
                var message = JsonConvert.DeserializeObject<T>(json);
                    
                await messageHandler(message);
                await args.CompleteMessageAsync(args.Message, cancellationToken);
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, object>
                {
                    { "Ex-Type", ex.GetType().Name },
                    { "Ex-Message", ex.Message },
                    { "Ex-StackTrace", ex.StackTrace ?? string.Empty }
                };
                await args.DeadLetterMessageAsync(args.Message, properties, cancellationToken);
            }
        };
                
        // processor.ProcessErrorAsync += (args) =>
        // {
        //     args.
        // };

        try
        {
            await processor.StartProcessingAsync(cancellationToken);
        
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "service bus consume error");
        }
        
        
        throw new OperationCanceledException();
    }
}