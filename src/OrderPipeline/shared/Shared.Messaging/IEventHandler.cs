namespace Shared.Messaging;

public interface IEventHandler<in T>
{
    Task HandleAsync(T @event);
}
