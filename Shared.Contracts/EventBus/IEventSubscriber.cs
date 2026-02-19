namespace Shared.Contracts.EventBus;

public interface IEventSubscriber
{
    Task Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
}