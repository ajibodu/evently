using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Evently.Core.Configurations;

namespace Evently.Azure.ServiceBus.Configurations;

public class ServiceBusConsumerConfiguration: IBaseConsumerConfiguration
{
    public string? TopicOrQueueName { get; set; }
    public string? SubscriptionName { get; set; }
    public bool CreateIfNotExist { get; set; }
    public ServiceBusProcessorOptions ServiceBusProcessorOptions { get; set; }
    public IList<CreateRuleOptions>? RuleOptions { get; set; }
    public string EventName { get; set; }
    public Type EventType { get; set; }
    public Type ConsumerType { get; set; }
    public string ConsumerName { get; set; }
    public RetryConfiguration? RetryConfiguration { get; set; }
}