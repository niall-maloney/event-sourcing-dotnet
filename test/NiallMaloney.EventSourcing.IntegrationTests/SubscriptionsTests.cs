using System.Reflection;
using EventStore.Client;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NiallMaloney.EventSourcing.Shared.Stubs;
using NiallMaloney.EventSourcing.Subscriptions;

namespace NiallMaloney.EventSourcing.IntegrationTests;

public class SubscriptionsTests
{
    [Fact]
    public async Task GivenSubscriptionRegistered_WhenEventsCommitted_ThenEventHandlerCalled()
    {
        //Arrange
        var cancellationToken = new CancellationToken();
        var services = new ServiceCollection();
        services.AddEventStore(Options.EventStore, new[] { Assembly.GetAssembly(typeof(UnitTested)) }!)
            .AddSubscriber<TestSubscriber>()
            .AddSingleton<ISubscriptionCursorRepository, InMemoryCursorRepository>()
            .AddSingleton<TestStore>();
        var provider = services.BuildServiceProvider();

        var subscriptionsManager = provider.GetRequiredService<IHostedService>();
        var cursorRepository = provider.GetRequiredService<ISubscriptionCursorRepository>();
        var store = provider.GetRequiredService<TestStore>();
        var client = provider.GetRequiredService<EventStoreClient>();

        await PrepareStream(client, cancellationToken);

        var subscriberName = "TestSubscriber";
        var categoryStreamName = "$ce-tests";
        var categoryStreamLength = await GetCategoryStreamLength(client, categoryStreamName);
        var expectedCategoryStreamLength = categoryStreamLength + 3;

        var testId = Guid.NewGuid().ToString();
        var streamId = $"tests-{testId}";
        var unitTestedEvents = new[] { new UnitTested(streamId), new UnitTested(streamId), new UnitTested(streamId) };

        //Set cursor to current stream length to avoid full projection
        await cursorRepository.UpsertSubscriptionCursor(subscriberName, categoryStreamName, categoryStreamLength);
        await subscriptionsManager.StartAsync(cancellationToken);

        //Act
        await client.AppendToStreamAsync(streamId, StreamRevision.None, unitTestedEvents, cancellationToken);
        await WaitForSubscriptionToCatchup(cursorRepository, subscriberName, categoryStreamName,
            expectedCategoryStreamLength);
        await subscriptionsManager.StopAsync(cancellationToken);

        //Assert
        store.Tests.ContainsKey(streamId).Should().BeTrue();
        store.Tests[streamId].Should().Be(3);
    }

    private static async Task PrepareStream(EventStoreClient client, CancellationToken cancellationToken)
    {
        // fire off random an event so category stream exists
        var streamId = $"tests-{Guid.NewGuid().ToString()}";
        await client.AppendToStreamAsync(streamId, StreamRevision.None, new[] { new UnitTested(streamId) },
            cancellationToken);
    }

    private static async Task WaitForSubscriptionToCatchup(
        ISubscriptionCursorRepository cursorRepository,
        string subscriberName,
        string categoryStreamName,
        ulong expectedCategoryStreamLength)
    {
        while (await GetSubscriptionCursor(cursorRepository, subscriberName, categoryStreamName) <
               expectedCategoryStreamLength)
        {
            await Task.Delay(50);
        }
    }

    private static async Task<ulong> GetSubscriptionCursor(
        ISubscriptionCursorRepository cursorRepository,
        string subscriberName,
        string categoryStreamName) =>
        await cursorRepository.GetSubscriptionCursor(subscriberName, categoryStreamName) ?? 0;

    private static async Task<ulong> GetCategoryStreamLength(EventStoreClient client, string streamName)
    {
        var enumerable = await client.ReadStreamAsync(streamName, StreamPosition.End, Direction.Backwards, 1,
            resolveLinkTos: true);

        if (enumerable is null)
        {
            return 0;
        }

        var envelope = await enumerable.SingleAsync();
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

[SubscriberName("TestSubscriber")]
[Subscription("$ce-tests")]
public class TestSubscriber : SubscriberBase
{
    private readonly TestStore _store;

    public TestSubscriber(TestStore store)
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
