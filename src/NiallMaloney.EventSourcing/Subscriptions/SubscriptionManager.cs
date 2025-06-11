using EventStore.Client;
using NiallMaloney.EventSourcing.DeadLetters;
using NodaTime.Extensions;

namespace NiallMaloney.EventSourcing.Subscriptions;

public class SubscriptionManager
{
    private readonly EventStoreClient _eventStore;
    private readonly ISubscriptionCursorRepository _cursorRepository;
    private readonly ISubscriber _subscriber;
    private readonly DeadLetterManager? _deadLetterManager;
    private readonly ISet<StreamSubscription> _subscriptions = new HashSet<StreamSubscription>();

    public SubscriptionManager(
        EventStoreClient eventStore,
        ISubscriptionCursorRepository cursorRepository,
        ISubscriber subscriber,
        DeadLetterManager? deadLetterManager)
    {
        _eventStore = eventStore;
        _cursorRepository = cursorRepository;
        _subscriber = subscriber;
        _deadLetterManager = deadLetterManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var subscriberType = _subscriber.GetType();
        var subscriberName = ISubscriber.GetSubscriberName(subscriberType);
        var attributes = ISubscriber.GetSubscriptionAttributes(subscriberType).Distinct();
        foreach (var attribute in attributes)
        {
            var streamName = attribute.StreamName;
            var currentCursor = await GetCursor(streamName);
            var start = currentCursor.HasValue
                ? FromStream.After(currentCursor.Value)
                : GetFromStream(attribute.Begin);
            var subscription = await _eventStore.SubscribeToStreamAsync(
                streamName, start, async (evnt, _) =>
                {
                    var (success, exception) = await _subscriber.Handle(evnt);
                    if (!success)
                    {
                        if (CanDeadLetter())
                        {
                            await StoreDeadLetter(evnt, subscriberName, streamName);
                        }
                        else
                        {
                            throw exception!;
                        }
                    }

                    await UpdateCursor(streamName, evnt);
                }, true, cancellationToken);
            _subscriptions.Add(subscription);
        }
    }

    private bool CanDeadLetter() => _deadLetterManager is not null;

    private async Task StoreDeadLetter(EventEnvelope<IEvent> evnt, string subscriberName,
        string streamName)
    {
        var subscriptionEventNumber = evnt.Metadata.StreamPosition;
        var deadLetterId = DeadLetterId(subscriberName, streamName, subscriptionEventNumber);
        await _deadLetterManager!.StoreDeadLetter(new EventDeadLetter(deadLetterId, subscriberName,
            streamName,
            subscriptionEventNumber, evnt.Event, evnt.Metadata, DateTime.UtcNow.ToInstant()));
    }

    public Task StopAsync(CancellationToken _)
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

    private static string DeadLetterId(string subscriberName, string streamName,
        ulong subscriptionEventNumber) =>
        DeadLetterManager.NewId(subscriberName, streamName, subscriptionEventNumber.ToString());
}
