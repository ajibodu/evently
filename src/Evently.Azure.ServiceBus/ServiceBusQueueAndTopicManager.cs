using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;

namespace Evently.Azure.ServiceBus;

public interface IServiceBusQueueAndTopicManager
{
    Task EnsureQueueExistsAsync(string queueName, CancellationToken cancellationToken);
    Task EnsureTopicSubscriptionExistsAsync(string topicName, string subscriptionName, CancellationToken cancellationToken);
    Task CreateSubscriptionRuleAsync(string topicName, string subscriptionName, IList<CreateRuleOptions> ruleOptions, CancellationToken cancellationToken);
}

public class ServiceBusQueueAndTopicManager(
    ServiceBusAdministrationClient adminClient,
    ILogger<ServiceBusQueueAndTopicManager> logger) : IServiceBusQueueAndTopicManager
{
    public async Task EnsureQueueExistsAsync(string queueName, CancellationToken cancellationToken)
    {
        if (!await adminClient.QueueExistsAsync(queueName, cancellationToken))
        {
            await adminClient.CreateQueueAsync(new CreateQueueOptions(queueName)
            {
                LockDuration = TimeSpan.FromMinutes(1),
                MaxDeliveryCount = 5,
                DefaultMessageTimeToLive = TimeSpan.FromDays(7)
            }, cancellationToken);
        }
    }
    
    public async Task EnsureTopicSubscriptionExistsAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
    {
        try
        {
            if (!await adminClient.TopicExistsAsync(topicName, cancellationToken))
            {
                var defaultRuleOption = new CreateRuleOptions
                {
                    Name = "$Default",
                    Filter = new SqlRuleFilter("1=1")
                };
                var options = new CreateSubscriptionOptions(topicName, subscriptionName);
                
                await adminClient.CreateTopicAsync(topicName, cancellationToken);
                await adminClient.CreateSubscriptionAsync(options, cancellationToken);
                await adminClient.CreateRuleAsync(topicName, subscriptionName, defaultRuleOption, cancellationToken);
                
                logger.LogDebug("Created topic {TopicName}", topicName);
            }
            else
            {
                logger.LogDebug("Topic {TopicName} already exists", topicName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating topic {TopicName}", topicName);
            throw;
        }
    }
    
    public async Task CreateSubscriptionRuleAsync(string topicName, string subscriptionName, IList<CreateRuleOptions> ruleOptions, CancellationToken cancellationToken)
    {
        try
        {
            if (!await adminClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken))
            {
                var options = new CreateSubscriptionOptions(topicName, subscriptionName);

                await adminClient.CreateSubscriptionAsync(options, cancellationToken);
                foreach (var ruleOption in ruleOptions)
                    await adminClient.CreateRuleAsync(topicName, subscriptionName, ruleOption, cancellationToken);
                
                logger.LogDebug("Created subscription {SubscriptionName} on topic {TopicName}", subscriptionName, topicName);
            }
            else
            {
                logger.LogDebug("Subscription {SubscriptionName} already exists on topic {TopicName}", subscriptionName, topicName);
                
                await DeleteRuleAsync(topicName, subscriptionName, "$Default", cancellationToken);
                foreach (var ruleOption in ruleOptions)
                    await CreateOrUpdateRuleAsync(topicName, subscriptionName, ruleOption, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating subscription {SubscriptionName}", subscriptionName);
            throw;
        }
    }

    private async Task CreateOrUpdateRuleAsync(string topicName, string subscriptionName, CreateRuleOptions ruleOption, CancellationToken cancellationToken)
    {
        await DeleteRuleAsync(topicName, subscriptionName, ruleOption.Name, cancellationToken);
        await adminClient.CreateRuleAsync(topicName, subscriptionName, ruleOption, cancellationToken);
    }
    
    private async Task DeleteRuleAsync(string topicName, string subscriptionName, string ruleName, CancellationToken cancellationToken)
    {
        try
        {
            await adminClient.DeleteRuleAsync(topicName, subscriptionName, ruleName, cancellationToken);
        }
        catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
        {
            // Rule didn't exist - that's fine
        }
    }
}