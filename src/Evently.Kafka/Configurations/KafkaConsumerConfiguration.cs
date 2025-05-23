using Confluent.Kafka;
using Evently.Core.Configurations;

namespace Evently.Kafka.Configurations;
    
public class KafkaConsumerConfiguration: IBaseConsumerConfiguration
{
    public string? TopicName { get; set; }
    public bool CreateTopicIfNotExist { get; set; }
    public ConsumerConfig ConsumerConfig { get; set; }
    public string EventName { get; set; }
    public Type EventType { get; set; }
    public Type ConsumerType { get; set; }
    public string ConsumerName { get; set; }
    public RetryConfiguration? RetryConfiguration { get; set; }
    public CreateIfNotExistControl? CreateIfNotExistControl { get; set; }
}