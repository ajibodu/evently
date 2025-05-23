using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Evently.Azure.ServiceBus.Configurations;
using Evently.Azure.ServiceBus.Constants;
using Evently.Core.Configurations;
using Newtonsoft.Json;

namespace Evently.Azure.ServiceBus.Builders;

public class ProducerBuilder(Configurations.Configuration configuration, ServiceBusClient serviceBusClient)
{
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senderCache = new();
    
    public void AddProducer<TEvent>(string? queueOrTopicName) where TEvent : new()
    {
        var fullName = typeof(TEvent).FullName;
        ArgumentException.ThrowIfNullOrEmpty(fullName);
        
        configuration.EventProducers.Add(new MessageProducer()
        {
            EventType = typeof(TEvent),
            SendAsync = async (evt, properties, cancellationToken) =>
            {
                if (evt is not TEvent typedEvent)
                    throw new InvalidCastException($"Expected type {typeof(TEvent).Name}, got {evt.GetType().Name}");

                var resolvedTopic = queueOrTopicName ?? Collection.NameFormaterResolver().Format<TEvent>();
                var sender = _senderCache.GetOrAdd(resolvedTopic, serviceBusClient.CreateSender);
                
                var message = new ServiceBusMessage(JsonConvert.SerializeObject(typedEvent))
                {
                    ContentType = "application/json",
                };
                
                message.ApplicationProperties.Add(Default.EventType, typeof(TEvent).FullName);
                if (properties != null)
                    foreach (var prop in properties)
                        message.ApplicationProperties.Add(prop.Key, prop.Value);
                
                await sender.SendMessageAsync(message, cancellationToken);
            }
        });
    }
}