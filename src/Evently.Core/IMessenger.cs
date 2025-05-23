namespace Evently.Core;

public interface IMessenger
{
    Task SendAsync<T>(T message, CancellationToken cancellationToken);
    Task SendAsync<T>(T message, Exception exception, CancellationToken cancellationToken);
    Task ConsumeAsync<T>(string consumerName, Func<T, Task> messageHandler, CancellationToken cancellationToken);
}