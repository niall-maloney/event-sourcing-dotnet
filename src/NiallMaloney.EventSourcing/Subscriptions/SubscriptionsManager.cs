using Microsoft.Extensions.Hosting;

namespace NiallMaloney.EventSourcing.Subscriptions;

public class SubscriptionsManager : IHostedService
{
    private readonly EventStoreClient _eventStore;
    private readonly IEnumerable<ISubscriber> _subscribers;
    private readonly ISubscriptionCursorRepository _cursorRepository;

    private readonly ISet<SubscriptionManager> _subscriptionManagers = new HashSet<SubscriptionManager>();

    public SubscriptionsManager(
        EventStoreClient eventStore,
        IEnumerable<ISubscriber> subscribers,
        ISubscriptionCursorRepository cursorRepository)
    {
        _eventStore = eventStore;
        _subscribers = subscribers;
        _cursorRepository = cursorRepository;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var tasks = new HashSet<Task>();
        foreach (var subscriber in _subscribers)
        {
            var subscriptionManager = new SubscriptionManager(_eventStore, _cursorRepository, subscriber);
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
