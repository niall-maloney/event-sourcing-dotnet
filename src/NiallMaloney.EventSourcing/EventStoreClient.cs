using EventStore.Client;

namespace NiallMaloney.EventSourcing;

using EventStore = EventStore.Client.EventStoreClient;

public class EventStoreClient
{
    private readonly EventStore _eventStore;
    private readonly EventSerializer _serializer;

    public EventStoreClient(EventStore eventStore, EventSerializer serializer)
    {
        _eventStore = eventStore;
        _serializer = serializer;
    }

    public Task<IAsyncEnumerable<EventEnvelope<IEvent>>?> ReadStreamFromBeginningAsync(
        string streamName,
        bool resolveLinkTos,
        CancellationToken cancellationToken = default)
    {
        return ReadStreamAsync(streamName, StreamPosition.Start, Direction.Forwards,
            resolveLinkTos: resolveLinkTos,
            cancellationToken: cancellationToken);
    }

    public async Task<IAsyncEnumerable<EventEnvelope<IEvent>>?> ReadStreamAsync(
        string streamName,
        StreamPosition position,
        Direction direction,
        long maxCount = long.MaxValue,
        bool resolveLinkTos = false,
        CancellationToken cancellationToken = default)
    {
        var result = _eventStore.ReadStreamAsync(direction, streamName, position,
            maxCount: maxCount,
            resolveLinkTos: resolveLinkTos, cancellationToken: cancellationToken);

        if (await result.ReadState is ReadState.StreamNotFound)
        {
            return null;
        }

        return result.Select(resolvedEvent =>
            new EventEnvelope<IEvent>(DeserializeEvent(resolvedEvent.Event),
                EventMetadata.FromResolvedEvent(resolvedEvent)));
    }

    public async Task AppendToStreamAsync(
        string streamName,
        StreamRevision expectedRevision,
        IReadOnlyCollection<IEvent> events,
        CancellationToken cancellationToken = default)
    {
        await _eventStore.AppendToStreamAsync(streamName, expectedRevision,
            events.Select(
                e => new EventData(Uuid.NewUuid(), IEvent.GetEventType(e.GetType()),
                    _serializer.Serialize(e))),
            cancellationToken: cancellationToken);
    }

    public async Task<StreamSubscription> SubscribeToStreamAsync(
        string streamName,
        FromStream start,
        Func<EventEnvelope<IEvent>, CancellationToken, Task> eventAppeared,
        bool resolveLinkTos = false,
        CancellationToken cancellationToken = default)
    {
        return await _eventStore.SubscribeToStreamAsync(streamName, start,
            (_, re, ct) =>
            {
                var evnt = new EventEnvelope<IEvent>(
                    DeserializeEvent(re.Event),
                    EventMetadata.FromResolvedEvent(re));

                return eventAppeared.Invoke(evnt, ct);
            }, resolveLinkTos,
            cancellationToken: cancellationToken);
    }

    private IEvent DeserializeEvent(EventRecord eventRecord) =>
        _serializer.Deserialize(eventRecord.Data, eventRecord.EventType);
}
