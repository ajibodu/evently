namespace Evently.Kafka.Configurations;

public class CreateIfNotExistControl(int numberOfPartitions = 1, int replicationFactor = 1)
{
    public int NumberOfPartitions { get; set; } = numberOfPartitions;
    public int ReplicationFactor { get; set; } = replicationFactor;
}