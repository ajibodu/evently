using Confluent.Kafka;
using Evently.Core;
using Evently.Core.Configurations;
using Evently.Core.Context;
using Evently.Kafka.Configurations;

namespace Evently.Kafka.Builders;

public class ConsumerBuilder(Configuration configuration, IEventlyContext context)
{
    public ConsumerBuilder AddConsumer<TEvent, TConsumer>(
        string? topic = null,
        int maxConsumer = 1, 
        ConsumerControl? control = null,
        RetryConfiguration? retryConfiguration = null,
        CreateIfNotExistControl? createIfNotExistControl = null)
        where TEvent : new()
        where TConsumer : IConsumer<TEvent>
    {
        
        AddConfiguration(topic, maxConsumer, control, retryConfiguration, typeof(TEvent), typeof(TConsumer), createIfNotExistControl);
        
        //register a dlq for the consumers queue
        new ProducerBuilder(configuration)
            .AddTopicProducer<string, TEvent>(topic, true);

        return this;
    }
    
    public ConsumerBuilder ConfigureConsumer<TConsumer>(
        string? topic = null,
        int maxConsumer = 1, 
        ConsumerControl? control = null,
        RetryConfiguration? retryConfiguration = null,
        CreateIfNotExistControl? createIfNotExistControl = null)
        where TConsumer : class
    {
        // Find the IConsumer<TEvent> interface implementation
        var consumerInterface = typeof(TConsumer).GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>));
        
        if (consumerInterface == null)
            throw new ArgumentException($"{typeof(TConsumer).Name} must implement IConsumer<TEvent>");
        // Get the TEvent type
        var eventType = consumerInterface.GetGenericArguments()[0];
        
        AddConfiguration(topic, maxConsumer, control, retryConfiguration, eventType, typeof(TConsumer), createIfNotExistControl);
        
        // Dynamically call AddTopicProducer<string, TEvent>(topic, true)
        var method = typeof(ProducerBuilder).GetMethod(nameof(ProducerBuilder.AddTopicProducer))!
            .MakeGenericMethod(typeof(string), eventType);
        
        var producerBuilder = new ProducerBuilder(configuration);
        method.Invoke(producerBuilder, [topic, true]);

        return this;
    }

    private void AddConfiguration(string? topic, int maxConsumer, ConsumerControl? control, RetryConfiguration? retryConfiguration, Type eventType, 
        Type consumerType, CreateIfNotExistControl? createIfNotExistControl)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration.BootStrapServers,
            GroupId = configuration.GroupId,
            AutoOffsetReset = control?.AutoOffsetReset ?? configuration.Control.AutoOffsetReset,
            SessionTimeoutMs = control?.SessionTimeoutMs ?? configuration.Control.SessionTimeoutMs,
            RetryBackoffMs = control?.RetryBackoffMs ?? configuration.Control.RetryBackoffMs,
            EnableAutoCommit = control?.EnableAutoCommit ?? configuration.Control.EnableAutoCommit,
            PartitionAssignmentStrategy = PartitionAssignmentStrategy.RoundRobin,
            SaslUsername = configuration.UserName
        };
        
        if (!string.IsNullOrWhiteSpace(configuration.UserName) && !string.IsNullOrWhiteSpace(configuration.Password))
        {
            consumerConfig.SaslMechanism = SaslMechanism.Plain;
            consumerConfig.SecurityProtocol = SecurityProtocol.SaslPlaintext;
            consumerConfig.SaslUsername = configuration.UserName;
            consumerConfig.SaslPassword = configuration.Password;
        }
        
        for (var i = 0; i < maxConsumer; i++)
        {
            consumerConfig.ClientId = $"{consumerType.Name}-{i}";
            context.RegisterConfiguration(new KafkaConsumerConfiguration{
                TopicName = topic,
                CreateTopicIfNotExist = createIfNotExistControl != null,
                EventName = eventType.Name,
                EventType = eventType,
                ConsumerType = consumerType,
                ConsumerName = $"{consumerType.Name}-{i}",
                ConsumerConfig = consumerConfig,
                RetryConfiguration = retryConfiguration,
                CreateIfNotExistControl = createIfNotExistControl
            });
        }
    }
}

