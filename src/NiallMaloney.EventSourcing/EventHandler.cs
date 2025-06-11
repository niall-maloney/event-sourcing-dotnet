namespace NiallMaloney.EventSourcing;

public class EventHandler
{
    private readonly IDictionary<string, Func<IEvent, EventMetadata, Task>> _handlers =
        new Dictionary<string, Func<IEvent, EventMetadata, Task>>();

    protected void When<T>(Func<T, EventMetadata, Task> handler) where T : class, IEvent
    {
        _handlers.Add(IEvent.GetEventType<T>(), async (e, em) => await handler((T)e, em));
    }

    protected async Task<EventHandlerResult> Handle<T>(EventEnvelope<T> envelope)
        where T : class, IEvent
    {
        if (!CanHandle(envelope, out var eventType))
        {
            throw new InvalidOperationException($"No handler for {eventType}.");
        }

        try
        {
            await _handlers[eventType].Invoke(envelope.Event, envelope.Metadata);
            return new EventHandlerResult(Success: true);
        }
        catch (Exception e)
        {
            return new EventHandlerResult(Success: false, Exception: e);
        }
    }

    protected bool CanHandle<T>(EventEnvelope<T> envelope, out string eventType)
        where T : class, IEvent
    {
        eventType = IEvent.GetEventType(envelope.Event.GetType());
        return _handlers.ContainsKey(eventType);
    }

    protected bool CanHandle<T>(EventEnvelope<T> envelope) where T : class, IEvent =>
        CanHandle(envelope, out var _);
}
