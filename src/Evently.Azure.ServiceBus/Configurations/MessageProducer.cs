namespace Evently.Azure.ServiceBus.Configurations;

public class MessageProducer
{
    public required Type EventType { get; set; }
    public required Func<object, Dictionary<string, object>?, CancellationToken, Task> SendAsync { get; set; }
}