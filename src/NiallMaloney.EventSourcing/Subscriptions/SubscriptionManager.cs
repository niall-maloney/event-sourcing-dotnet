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
        var streamNames = ISubscriber.GetSubscriptionAttributes(_subscriber.GetType())
            .Select(subscriptionAttribute => subscriptionAttribute.StreamName)
            .Distinct();

        foreach (var streamName in streamNames)
        {
            var currentCursor = await GetCursor(streamName);
            var start = currentCursor.HasValue ? FromStream.After(currentCursor.Value) : FromStream.Start;
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

    private async Task<ulong?> GetCursor(string streamName)
    {
        var subscriberName = ISubscriber.GetSubscriberName(_subscriber.GetType());
        return await _cursorRepository.GetSubscriptionCursor(subscriberName, streamName);
    }

    private async Task UpdateCursor(string streamName, EventEnvelope<IEvent> evnt) =>
        await _cursorRepository.UpsertSubscriptionCursor(ISubscriber.GetSubscriberName(_subscriber.GetType()),
            streamName, evnt.Metadata.AggregatedStreamPosition);
}
