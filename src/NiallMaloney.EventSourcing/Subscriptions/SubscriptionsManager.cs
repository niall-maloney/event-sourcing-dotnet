using Microsoft.Extensions.Hosting;
using NiallMaloney.EventSourcing.DeadLetters;

namespace NiallMaloney.EventSourcing.Subscriptions;

public class SubscriptionsManager : IHostedService
{
    private readonly EventStoreClient _eventStore;
    private readonly IEnumerable<ISubscriber> _subscribers;
    private readonly ISubscriptionCursorRepository _cursorRepository;
    private readonly DeadLetterManager? _deadLetterManager;

    private readonly ISet<SubscriptionManager> _subscriptionManagers =
        new HashSet<SubscriptionManager>();

    public SubscriptionsManager(
        EventStoreClient eventStore,
        IEnumerable<ISubscriber> subscribers,
        ISubscriptionCursorRepository cursorRepository,
        DeadLetterManager? deadLetterManager = null)
    {
        _eventStore = eventStore;
        _subscribers = subscribers;
        _cursorRepository = cursorRepository;
        _deadLetterManager = deadLetterManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var tasks = new HashSet<Task>();
        foreach (var subscriber in _subscribers)
        {
            var subscriptionManager =
                new SubscriptionManager(_eventStore, _cursorRepository, subscriber,
                    _deadLetterManager);
            _subscriptionManagers.Add(subscriptionManager);
            tasks.Add(subscriptionManager.StartAsync(cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var tasks = new HashSet<Task>();
        foreach (var subscriptionManager in _subscriptionManagers)
        {
            tasks.Add(subscriptionManager.StopAsync(cancellationToken));
        }

        await Task.WhenAll(tasks);
    }
}
