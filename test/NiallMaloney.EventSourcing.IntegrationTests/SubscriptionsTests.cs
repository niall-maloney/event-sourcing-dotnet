using System.Reflection;
using EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NiallMaloney.EventSourcing.IntegrationTests.Events;
using NiallMaloney.EventSourcing.IntegrationTests.Subscribers;
using NiallMaloney.EventSourcing.Shared.Stubs;
using NiallMaloney.EventSourcing.Subscriptions;
using Shouldly;

namespace NiallMaloney.EventSourcing.IntegrationTests;

public class SubscriptionsTests
{
    [Fact]
    public async Task GivenSubscriptionRegistered_WhenEventsCommitted_ThenEventHandlerCalled()
    {
        //Arrange
        var cancellationToken = CancellationToken.None;
        var services = new ServiceCollection();
        services.AddEventStore(Options.EventStore, [Assembly.GetAssembly(typeof(UnitTested))!]!)
            .AddSubscriber<TestSubscriberFromStreamStart>()
            .AddSingleton<ISubscriptionCursorRepository, InMemoryCursorRepository>()
            .AddSingleton<TestStore>();
        var provider = services.BuildServiceProvider();

        var subscriptionsManager = provider.GetRequiredService<IHostedService>();
        var cursorRepository = provider.GetRequiredService<ISubscriptionCursorRepository>();
        var store = provider.GetRequiredService<TestStore>();
        var client = provider.GetRequiredService<EventStoreClient>();

        await PrepareCategoryStream(client, cancellationToken);

        var subscriberName = TestSubscriberFromStreamStart.SubscriberName;
        var categoryStreamName = TestSubscriberFromStreamStart.CategoryStreamName;

        var testId = Guid.NewGuid().ToString();
        var streamId = $"tests-{testId}";
        IEvent[] unitTestedEvents =
            [new UnitTested(testId), new UnitTested(testId), new UnitTested(testId)];

        var categoryStreamLength = await GetCategoryStreamPosition(client, categoryStreamName);
        var expectedCategoryStreamLength = categoryStreamLength + (ulong)unitTestedEvents.Length;

        //Set cursor to current stream length to avoid full projection
        await cursorRepository.UpsertSubscriptionCursor(subscriberName, categoryStreamName,
            categoryStreamLength);
        await subscriptionsManager.StartAsync(cancellationToken);

        //Act
        await client.AppendToStreamAsync(streamId, StreamRevision.None, unitTestedEvents,
            cancellationToken);
        await WaitForSubscriptionToCatchup(cursorRepository, subscriberName, categoryStreamName,
            expectedCategoryStreamLength);
        await subscriptionsManager.StopAsync(cancellationToken);

        //Assert
        store.Tests.ContainsKey(testId).ShouldBeTrue();
        store.Tests[testId].ShouldBe(3);
    }

    [Fact]
    public async Task
        GivenSubscriptionRegistered_WithCursorFromStreamEnd_WhenEventsCommitted_ThenEventHandlerCalled()
    {
        //Arrange
        var cancellationToken = CancellationToken.None;
        var services = new ServiceCollection();
        services.AddEventStore(Options.EventStore, [Assembly.GetAssembly(typeof(UnitTested))!]!)
            .AddSubscriber<TestSubscriberFromStreamEnd>()
            .AddSingleton<ISubscriptionCursorRepository, InMemoryCursorRepository>()
            .AddSingleton<TestStore>();
        var provider = services.BuildServiceProvider();

        var subscriptionsManager = provider.GetRequiredService<IHostedService>();
        var cursorRepository = provider.GetRequiredService<ISubscriptionCursorRepository>();
        var store = provider.GetRequiredService<TestStore>();
        var client = provider.GetRequiredService<EventStoreClient>();

        await PrepareCategoryStream(client, cancellationToken);

        var subscriberName = TestSubscriberFromStreamEnd.SubscriberName;
        var categoryStreamName = TestSubscriberFromStreamEnd.CategoryStreamName;

        var testId = Guid.NewGuid().ToString();
        var streamId = $"tests-{testId}";
        IEvent[] unitTestedEvents =
            [new UnitTested(testId), new UnitTested(testId), new UnitTested(testId)];

        var categoryStreamLength = await GetCategoryStreamPosition(client, categoryStreamName);
        var expectedCategoryStreamLength = categoryStreamLength + (ulong)unitTestedEvents.Length;

        //We don't need to set cursor to current stream length as we are subscribing from end
        await subscriptionsManager.StartAsync(cancellationToken);

        //Act
        await client.AppendToStreamAsync(streamId, StreamRevision.None, unitTestedEvents,
            cancellationToken);
        await WaitForSubscriptionToCatchup(cursorRepository, subscriberName, categoryStreamName,
            expectedCategoryStreamLength);
        await subscriptionsManager.StopAsync(cancellationToken);

        //Assert
        store.Tests.ContainsKey(testId).ShouldBeTrue();
        store.Tests[testId].ShouldBe(3);
    }

    private static async Task PrepareCategoryStream(EventStoreClient client,
        CancellationToken cancellationToken)
    {
        if ((await GetCategoryStreamPosition(client, "$ce-tests")) != 0)
        {
            return;
        }

        // fire off random event so category stream exists
        var streamId = $"tests-{Guid.NewGuid().ToString()}";
        await client.AppendToStreamAsync(streamId, StreamRevision.None, [new UnitTested(streamId)],
            cancellationToken);

        while ((await GetCategoryStreamLength(client, "$ce-deadletter_tests")) < 1)
        {
            await Task.Delay(50, cancellationToken);
        }
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

    private static async Task<ulong> GetCategoryStreamPosition(EventStoreClient client,
        string streamName)
    {
        return await GetAggregatedStreamPosition(client, streamName, getLength: false);
    }

    private static async Task<ulong> GetCategoryStreamLength(EventStoreClient client,
        string streamName)
    {
        return await GetAggregatedStreamPosition(client, streamName);
    }

    private static async Task<ulong> GetAggregatedStreamPosition(EventStoreClient client,
        string streamName, bool getLength = true)
    {
        var enumerable = await client.ReadStreamAsync(streamName, StreamPosition.End,
            Direction.Backwards, 1,
            resolveLinkTos: true);

        if (enumerable is null)
        {
            return 0;
        }

        var envelope = await enumerable.SingleAsync();
        return getLength
            ? envelope.Metadata.AggregatedStreamPosition + 1
            : envelope.Metadata.AggregatedStreamPosition;
    }
}
