namespace Evently.Core;

public interface IConsumer<TEvent>
{
    Task HandleAsync(ConsumerContext<TEvent> context, CancellationToken cancellationToken);
}

