using Evently.Core.Configurations;
using Microsoft.Extensions.Hosting;

namespace Evently.Azure.ServiceBus.HostedServices;

public class Initializer(Configurations.Configuration config, IServiceBusQueueAndTopicManager queueAndTopicManager) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (config.CreateTopicOrQueueIfNotExist)
            foreach (var consumerConfiguration in config.ServiceBusConsumerConfigurations)
            {
                var topicOrQueueName = consumerConfiguration.TopicOrQueueName ?? Collection.NameFormaterResolver().Format(consumerConfiguration.EventType);
                if(string.IsNullOrEmpty(consumerConfiguration.SubscriptionName))
                    await queueAndTopicManager.EnsureQueueExistsAsync(topicOrQueueName, cancellationToken);
                else
                {
                    await queueAndTopicManager.EnsureTopicSubscriptionExistsAsync(topicOrQueueName, consumerConfiguration.SubscriptionName, cancellationToken);
                    if(consumerConfiguration.RuleOptions != null)
                        await queueAndTopicManager.CreateSubscriptionRuleAsync(topicOrQueueName, consumerConfiguration.SubscriptionName, consumerConfiguration.RuleOptions, cancellationToken);
                }
            }
        
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}