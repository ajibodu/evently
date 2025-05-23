using Evently.Core.Configurations;
using Evently.Core.Extensions;
using Microsoft.Extensions.Hosting;

namespace Evently.Kafka.HostedServices;

public class Initializer(IKafkaTopicManager topicManager, Configurations.Configuration config) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (config.CreateTopicIfNotExist)
        {
            foreach (var kafkaConsumerConfiguration in config.KafkaConsumerConfigurations)
            {
                await topicManager.CreateTopicIfNotExistsAsync(config.BootStrapServers, kafkaConsumerConfiguration.TopicName!);
            }
        }
        
        foreach (var kafkaConsumerConfiguration in config.KafkaConsumerConfigurations)
        {
            await topicManager.CreateTopicIfNotExistsAsync(config.BootStrapServers, kafkaConsumerConfiguration.TopicName!.ToDlQ());
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}