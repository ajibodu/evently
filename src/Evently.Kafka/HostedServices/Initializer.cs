using Evently.Core.Configurations;
using Evently.Core.Extensions;
using Microsoft.Extensions.Hosting;

namespace Evently.Kafka.HostedServices;

public class Initializer(IKafkaTopicManager topicManager, Configurations.Configuration config) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var kafkaConsumerConfiguration in config.KafkaConsumerConfigurations)
        {
            if(kafkaConsumerConfiguration.CreateIfNotExistControl != null)
            {
                await topicManager.CreateTopicIfNotExistsAsync(config.BootStrapServers, kafkaConsumerConfiguration.TopicName!, kafkaConsumerConfiguration.CreateIfNotExistControl);
            }
            await topicManager.CreateTopicIfNotExistsAsync(config.BootStrapServers, kafkaConsumerConfiguration.TopicName!.ToDlQ());
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}