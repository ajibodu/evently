using Confluent.Kafka;

namespace Evently.Kafka.Configurations;

public class TopicProducer
{
    public required Type EventType { get; set; }
    public required Func<object, object?, Headers?, CancellationToken, Task> SendAsync { get; set; }
    public bool IsDlQ { get; set; }
}