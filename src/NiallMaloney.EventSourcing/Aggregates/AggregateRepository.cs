using EventStore.Client;

namespace NiallMaloney.EventSourcing.Aggregates;

public class AggregateRepository
{
    private readonly EventStoreClient _eventStore;

    public AggregateRepository(EventStoreClient eventStore) => _eventStore = eventStore;

    public async Task<T> LoadAggregate<T>(string id) where T : Aggregate, new()
    {
        var streamName = AggregateUtilities.GetStreamName<T>(id);
        var events = await _eventStore.ReadStreamFromBeginningAsync(streamName, true);

        T aggregate = new() { Id = id };
        if (events is null)
        {
            return aggregate;
        }

        await foreach (var evnt in events)
        {
            aggregate.ReplayEvent(evnt);
        }

        return aggregate;
    }

    public async Task SaveAggregate<T>(T aggregate) where T : Aggregate
    {
        var streamName = AggregateUtilities.GetStreamName<T>(aggregate.Id);

        var unsavedEvents = aggregate.DequeueUnsavedEvents();
        if (!unsavedEvents.Any())
        {
            return;
        }

        await _eventStore.AppendToStreamAsync(
            streamName: streamName,
            expectedRevision: aggregate.LastSavedEventVersion() ?? StreamRevision.None,
            events: unsavedEvents);
    }
}
