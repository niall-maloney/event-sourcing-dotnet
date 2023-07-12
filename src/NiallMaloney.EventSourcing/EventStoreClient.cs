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
        return ReadStreamAsync(streamName, StreamPosition.Start, Direction.Forwards, resolveLinkTos, cancellationToken);
    }

    public async Task<IAsyncEnumerable<EventEnvelope<IEvent>>?> ReadStreamAsync(
        string streamName,
        StreamPosition position,
        Direction direction,
        bool resolveLinkTos,
        CancellationToken cancellationToken = default)
    {
        var result = _eventStore.ReadStreamAsync(direction, streamName, position, resolveLinkTos: resolveLinkTos,
            cancellationToken: cancellationToken);

        if (await result.ReadState is ReadState.StreamNotFound)
        {
            return null;
        }

        return result.Select(resolvedEvent => new EventEnvelope<IEvent>(_serializer.Deserialize(resolvedEvent.Event),
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
                e => new EventData(Uuid.NewUuid(), IEvent.GetEventType(e.GetType()), _serializer.Serialize(e))),
            cancellationToken: cancellationToken);
    }
}