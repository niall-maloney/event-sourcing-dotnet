using System.Collections.Immutable;

namespace NiallMaloney.EventSourcing.Aggregates;

public abstract class Aggregate
{
    private readonly string? _id;

    public string Id
    {
        get => _id ?? throw new InvalidOperationException("No ID set.");
        init => _id = value;
    }

    public int Version { get; protected set; }

    private ImmutableArray<EventEnvelope<IEvent>> _savedEvents =
        ImmutableArray<EventEnvelope<IEvent>>.Empty;

    public ImmutableArray<EventEnvelope<IEvent>> SavedEvents => _savedEvents;

    private readonly Queue<IEvent> _unsavedEvents = new();
    public ImmutableArray<IEvent> UnsavedEvents => _unsavedEvents.ToImmutableArray();

    private readonly IDictionary<Type, Action<IEvent>> _handlers =
        new Dictionary<Type, Action<IEvent>>();

    public ImmutableArray<IEvent> DequeueUnsavedEvents()
    {
        var unsavedEvents = UnsavedEvents;
        _unsavedEvents.Clear();
        return unsavedEvents;
    }

    protected void When<T>(Action<T> handler) where T : class, IEvent
    {
        _handlers.Add(typeof(T),
            e => handler(e as T ??
                         throw new InvalidOperationException(
                             $"Cannot apply event, as event cannot be cast to \"{typeof(T).Name}\".")));
    }

    protected void RaiseEvent(IEvent evnt)
    {
        ApplyEvent(evnt);
        _unsavedEvents.Enqueue(evnt);
    }

    public void ReplayEvent(EventEnvelope<IEvent> eventEnvelope)
    {
        ApplyEvent(eventEnvelope.Event);
        _savedEvents = _savedEvents.Add(eventEnvelope);
    }

    private void ApplyEvent(IEvent evnt)
    {
        var eventType = evnt.GetType();
        if (!_handlers.TryGetValue(eventType, out var handler))
        {
            throw new InvalidOperationException($"No handler for {eventType}.");
        }

        handler.Invoke(evnt);
        Version++;
    }
}
