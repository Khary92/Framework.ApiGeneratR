namespace Shared.Contracts.EventBus;

public class EventService : IEventSubscriber, IEventPublisher
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly Lock _lock = new();

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        List<Delegate> handlersCopy;

        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var handlers)) return;
            handlersCopy = new List<Delegate>(handlers);
        }

        var tasks = handlersCopy.Cast<Func<TEvent, Task>>()
            .Select(h => h(@event));
        await Task.WhenAll(tasks);
    }

    public Task Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers = [];
                _handlers[typeof(TEvent)] = handlers;
            }

            handlers.Add(handler);
        }

        return Task.CompletedTask;
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out var handlers)) return;

            handlers.Remove(handler);

            if (handlers.Count == 0) _handlers.Remove(typeof(TEvent));
        }
    }
}