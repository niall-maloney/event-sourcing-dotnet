using NiallMaloney.EventSourcing.IntegrationTests.Events;
using NiallMaloney.EventSourcing.Subscriptions;

namespace NiallMaloney.EventSourcing.IntegrationTests.Subscribers;

[SubscriberName(SubscriberName)]
[Subscription(CategoryStreamName)]
public class TestSubscriberFromStreamStart : SubscriberBase
{
    public const string SubscriberName = "TestSubscriberFromStreamStart";
    public const string CategoryStreamName = "$ce-tests";

    private readonly TestStore _store;

    public TestSubscriberFromStreamStart(TestStore store)
    {
        _store = store;
        When<UnitTested>(Handle);
    }

    private Task Handle(UnitTested evnt, EventMetadata metadata)
    {
        _store.Add(evnt.TestId);
        return Task.CompletedTask;
    }
}
