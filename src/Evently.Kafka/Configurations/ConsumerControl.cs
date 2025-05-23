using Confluent.Kafka;

namespace Evently.Kafka.Configurations;

public class ConsumerControl
{
    public int SessionTimeoutMs { get; set; }
    public int MessageSendMaxRetries { get; set; }
    public int RetryBackoffMs { get; set; }
    public bool EnableAutoCommit { get; set; }
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
}