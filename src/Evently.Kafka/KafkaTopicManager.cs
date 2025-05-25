using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Evently.Kafka.Configurations;
using Microsoft.Extensions.Logging;

namespace Evently.Kafka;

public interface IKafkaTopicManager
{
    Task CreateTopicIfNotExistsAsync(
        string bootstrapServers,
        string topicName,
        CreateIfNotExistControl? createIfNotExistControl = null,
        Dictionary<string, string>? config = null);
}
public class KafkaTopicManager(ILogger<KafkaTopicManager> logger) : IKafkaTopicManager
{
    public async Task CreateTopicIfNotExistsAsync(
        string bootstrapServers,
        string topicName, 
        CreateIfNotExistControl? createIfNotExistControl = null,
        Dictionary<string, string>? config = null)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers })
        .SetLogHandler((_, message) => KafkaLoggingHelper.LogKafkaMessage(message))
        .SetErrorHandler((_, error) => KafkaLoggingHelper.LogKafkaError(error))
        .Build();
        createIfNotExistControl ??= new CreateIfNotExistControl();

        try
        {
            // Get metadata to check if topic exists
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            var topicExists = metadata.Topics.Any(t => t.Topic == topicName);

            if (!topicExists)
            {
                logger.LogDebug($"Topic '{topicName}' doesn't exist. Creating...");
                
                await adminClient.CreateTopicsAsync(new List<TopicSpecification>
                {
                    new TopicSpecification
                    {
                        Name = topicName,
                        NumPartitions = createIfNotExistControl.NumberOfPartitions,
                        ReplicationFactor = (short)createIfNotExistControl.ReplicationFactor,
                        Configs = config ?? new Dictionary<string, string>()
                    }
                });
                
                logger.LogDebug($"Topic '{topicName}' created successfully.");
            }
            else
            {
                logger.LogDebug($"Topic '{topicName}' already exists.");
            }
        }
        catch (KafkaException e)
        {
            logger.LogError(e, $"An error occurred working with topic: {topicName}");
        }
    }
}