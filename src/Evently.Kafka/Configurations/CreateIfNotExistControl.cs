namespace Evently.Kafka.Configurations;

public class CreateIfNotExistControl
{
    public int NumberOfPartitions { get; set; }
    public int ReplicationFactor { get; set; }
}