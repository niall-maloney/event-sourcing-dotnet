using NiallMaloney.EventSourcing.IntegrationTests.Events;
using NiallMaloney.EventSourcing.Subscriptions;

namespace NiallMaloney.EventSourcing.IntegrationTests.Subscribers;

[SubscriberName(SubscriberName)]
[Subscription(CategoryStreamName, begin: CursorFromStream.Start)]
public class TestDeadLetterSubscriber : SubscriberBase
{
    private readonly TestStore _store;
    private readonly DeadLetterFlags _flags;
    public const string SubscriberName = "TestDeadLetterSubscriber";
    public const string CategoryStreamName = "$ce-deadletter_tests";

    public TestDeadLetterSubscriber(TestStore store, DeadLetterFlags flags)
    {
        _store = store;
        _flags = flags;
        When<UnitTested>(Handle);
    }

    private Task Handle(UnitTested evnt, EventMetadata metadata)
    {
        var testId = evnt.TestId;
        if (_flags.ShouldDeadLetter(testId))
        {
            throw new Exception("Test dead letter");
        }

        _store.Add(testId);
        return Task.CompletedTask;
    }
}

public class DeadLetterFlags
{
    private Dictionary<string, bool> Flags { get; } = new();
    public bool ShouldDeadLetter(string testId) => Flags.TryGetValue(testId, out var flag) && flag;
    public void SetFlag(string testId, bool flag) => Flags[testId] = flag;
}
