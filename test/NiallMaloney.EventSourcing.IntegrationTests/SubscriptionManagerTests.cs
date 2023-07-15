using System.Reflection;
using EventStore.Client;
using FluentAssertions;
using NiallMaloney.EventSourcing.Shared.Stubs;
using NiallMaloney.EventSourcing.Subscriptions;

namespace NiallMaloney.EventSourcing.IntegrationTests;

public class SubscriptionManagerTests
{
    private readonly EventStoreClient _client;

    private readonly EventStoreClientOptions _eventStoreOptions =
        new("esdb+discover://localhost:2113?tls=false&keepAliveTimeout=10000&keepAliveInterval=10000");

    private readonly InMemoryCursorRepository _cursorRepository;

    public SubscriptionManagerTests()
    {
        var eventStore =
            new EventStore.Client.EventStoreClient(
                EventStoreClientSettings.Create(_eventStoreOptions.ConnectionString));
        var serializer = new EventSerializer(Assembly.GetAssembly(typeof(UnitTested))!);
        _client = new EventStoreClient(eventStore, serializer);
        _cursorRepository = new InMemoryCursorRepository();
    }

    [Fact]
    public async Task GivenSubscription_WhenEventsCommitted_ThenEventHandlerCalled()
    {
        //Arrange
        var store = new TestStore();
        var subscriber = new TestProjection(store);
        var cancellationToken = new CancellationToken();
        var subscriptionManager = new SubscriptionManager(_client, _cursorRepository, subscriber);

        var subscriberName = "TestProjection";
        var categoryStreamName = "$ce-tests";
        var categoryStreamLength = await GetCategoryStreamLength();
        var expectedCategoryStreamLength = categoryStreamLength + 3;

        var testId = Guid.NewGuid().ToString();
        var streamId = $"tests-{testId}";
        var unitTestedEvents = new[] { new UnitTested(streamId), new UnitTested(streamId), new UnitTested(streamId) };

        //Set cursor to current stream to avoid full projection
        await _cursorRepository.UpsertSubscriptionCursor(subscriberName, categoryStreamName, categoryStreamLength);
        await subscriptionManager.StartAsync(cancellationToken);

        //Act
        await _client.AppendToStreamAsync(streamId, StreamRevision.None, unitTestedEvents, cancellationToken);
        await WaitForSubscriptionToCatchup(subscriberName, categoryStreamName, expectedCategoryStreamLength);

        //Assert
        store.Tests.ContainsKey(streamId).Should().BeTrue();
        store.Tests[streamId].Should().Be(3);
    }

    private async Task WaitForSubscriptionToCatchup(
        string subscriberName,
        string categoryStreamName,
        ulong expectedCategoryStreamLength)
    {
        while (await _cursorRepository.GetSubscriptionCursor(subscriberName, categoryStreamName) <
               expectedCategoryStreamLength)
        {
            await Task.Delay(50);
        }
    }

    private async Task<ulong> GetCategoryStreamLength()
    {
        var enumerable = await _client.ReadStreamAsync("$ce-tests", StreamPosition.End, Direction.Backwards, 1,
            resolveLinkTos: true);
        var envelope = await enumerable!.SingleAsync();
        return envelope.Metadata.AggregatedStreamPosition;
    }
}

public class TestStore
{
    public IDictionary<string, int> Tests { get; } = new Dictionary<string, int>();

    public void Add(string key)
    {
        if (Tests.ContainsKey(key))
        {
            Tests[key]++;
        }
        else
        {
            Tests.Add(key, 1);
        }
    }
}

[SubscriberName("TestProjection")]
[Subscription("$ce-tests")]
public class TestProjection : SubscriberBase
{
    private readonly TestStore _store;

    public TestProjection(TestStore store)
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
