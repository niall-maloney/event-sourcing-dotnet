using NiallMaloney.EventSourcing.IntegrationTests.Events;
using NiallMaloney.EventSourcing.Subscriptions;

namespace NiallMaloney.EventSourcing.IntegrationTests.Subscribers;

[SubscriberName(SubscriberName)]
[Subscription(CategoryStreamName, begin: CursorFromStream.End)]
public class TestSubscriberFromStreamEnd : SubscriberBase
{
    public const string SubscriberName = "TestSubscriberFromStreamEnd";
    public const string CategoryStreamName = "$ce-tests";

    private readonly TestStore _store;

    public TestSubscriberFromStreamEnd(TestStore store)
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
