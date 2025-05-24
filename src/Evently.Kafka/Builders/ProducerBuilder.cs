using System.Net;
using Confluent.Kafka;
using Evently.Core;
using Evently.Core.Configurations;
using Evently.Core.Extensions;
using Evently.Kafka.Configurations;

namespace Evently.Kafka.Builders;

public class ProducerBuilder(Configuration configuration)
{
    public ProducerBuilder AddProducer<TKey, TEvent>(string? topic = null, bool isDlQ = false) where TEvent : class
    {
        AddTopicProducer<TKey, TEvent>(topic, isDlQ);

        return this;
    }

    public void AddTopicProducer<TKey, TEvent>(string? topic, bool isDlQ) where TEvent : class
    {
        var producerConfig =  new ProducerConfig
        {
            BootstrapServers = configuration.BootStrapServers,
            ClientId = configuration.GroupId,
            Acks = Acks.All,
            MessageTimeoutMs = configuration.Control.SessionTimeoutMs,
            RetryBackoffMs = configuration.Control.RetryBackoffMs,
            MessageSendMaxRetries = configuration.Control.MessageSendMaxRetries,
        };
        
        if (!string.IsNullOrWhiteSpace(configuration.UserName) && !string.IsNullOrWhiteSpace(configuration.Password))
        {
            producerConfig.SaslMechanism = SaslMechanism.Plain;
            producerConfig.SecurityProtocol = SecurityProtocol.SaslPlaintext;
            producerConfig.SaslUsername = configuration.UserName;
            producerConfig.SaslPassword = configuration.Password;
        }
        
        var producer = new ProducerBuilder<TKey?, TEvent>(producerConfig)
            .SetValueSerializer(new NewtonsoftJsonSerializer<TEvent>())
            .Build();
        
        configuration.TopicProducers.Add(new TopicProducer()
        {
            EventType = typeof(TEvent),
            SendAsync = async (key, evt,  headers, cancellationToken) =>
            {
                if (evt is not TEvent typedEvent)
                    throw new InvalidCastException($"Expected type {typeof(TEvent).Name}, got {evt.GetType().Name}");

                var resolvedTopic = topic ?? Collection.NameFormaterResolver().Format<TEvent>();
                if(isDlQ) resolvedTopic = resolvedTopic.ToDlQ();
                
                await producer.ProduceAsync(resolvedTopic, new Message<TKey?, TEvent>
                {
                    Key = key != null ? (TKey)key : default,
                    Value = typedEvent,
                    Headers = headers
                }, cancellationToken);
            },
            IsDlQ = isDlQ
        });
    }
}