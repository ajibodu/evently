namespace Evently.Core.Configurations;

// public record BaseConsumerConfiguration(
//     string EventName, 
//     Type EventType, 
//     Type ConsumerType, 
//     string ConsumerName,
//     RetryConfiguration? RetryConfiguration);
public class BaseConsumerConfiguration : IBaseConsumerConfiguration
{
    public string EventName { get; set; }
    public Type EventType { get; set; }
    public Type ConsumerType { get; set; }
    public string ConsumerName { get; set; }
    public RetryConfiguration? RetryConfiguration { get; set; }
}

public interface IBaseConsumerConfiguration
{
    public string EventName { get; set; }
    public Type EventType { get; set; }
    public Type ConsumerType { get; set; }
    public string ConsumerName { get; set; }
    public RetryConfiguration? RetryConfiguration { get; set; }
}