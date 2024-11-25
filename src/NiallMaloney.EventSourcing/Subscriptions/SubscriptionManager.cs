using EventStore.Client;

namespace NiallMaloney.EventSourcing.Subscriptions;

public class SubscriptionManager
{
    private readonly EventStoreClient _eventStore;
    private readonly ISubscriptionCursorRepository _cursorRepository;
    private readonly ISubscriber _subscriber;
    private readonly ISet<StreamSubscription> _subscriptions = new HashSet<StreamSubscription>();

    public SubscriptionManager(
        EventStoreClient eventStore,
        ISubscriptionCursorRepository cursorRepository,
        ISubscriber subscriber)
    {
        _eventStore = eventStore;
        _cursorRepository = cursorRepository;
        _subscriber = subscriber;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var attributes = ISubscriber.GetSubscriptionAttributes(_subscriber.GetType())
            .Distinct();

        foreach (var attribute in attributes)
        {
            var streamName = attribute.StreamName;
            var currentCursor = await GetCursor(streamName);
            var start = currentCursor.HasValue ? FromStream.After(currentCursor.Value) : GetFromStream(attribute.Begin);
            var subscription = await _eventStore.SubscribeToStreamAsync(
                streamName, start, async (evnt, _) =>
                {
                    await _subscriber.Handle(evnt);
                    await UpdateCursor(streamName, evnt);
                }, true, cancellationToken);
            _subscriptions.Add(subscription);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var streamSubscription in _subscriptions)
        {
            streamSubscription.Dispose();
        }

        return Task.CompletedTask;
    }

    private async Task<ulong?> GetCursor(string streamName) =>
        await _cursorRepository.GetSubscriptionCursor(GetSubscriberName(), streamName);

    private async Task UpdateCursor(string streamName, EventEnvelope<IEvent> evnt) =>
        await _cursorRepository.UpsertSubscriptionCursor(GetSubscriberName(), streamName,
            evnt.Metadata.AggregatedStreamPosition);

    private static FromStream GetFromStream(CursorFromStream begin) => begin switch
    {
        CursorFromStream.Start => FromStream.Start,
        CursorFromStream.End   => FromStream.End,
        _                      => throw new ArgumentOutOfRangeException(nameof(begin), begin, null),
    };

    private string GetSubscriberName() => ISubscriber.GetSubscriberName(_subscriber.GetType());
}
