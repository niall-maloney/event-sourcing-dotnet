using System.Reflection;
using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NiallMaloney.EventSourcing.DeadLetters;
using NiallMaloney.EventSourcing.IntegrationTests.Events;
using NiallMaloney.EventSourcing.IntegrationTests.Subscribers;
using NiallMaloney.EventSourcing.Shared.Stubs;
using NiallMaloney.EventSourcing.Subscriptions;
using Shouldly;
using Xunit.Abstractions;

namespace NiallMaloney.EventSourcing.IntegrationTests;

public class DeadLetterTests
{
    private readonly ITestOutputHelper _output;

    public DeadLetterTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task GivenSubscriptionRegistered_WhenHandlerThrows_ThenEventDeadLetters()
    {
        //Arrange
        var cancellationToken = CancellationToken.None;
        var services = new ServiceCollection();
        services.AddEventStore(Options.KurrentDB, [Assembly.GetAssembly(typeof(UnitTested))!]!)
            .AddSubscriber<TestDeadLetterSubscriber>()
            .AddSingleton<ISubscriptionCursorRepository, InMemoryCursorRepository>()
            .AddDeadLetters(_ => new InMemoryDeadLetterRepository())
            .AddSingleton<TestStore>();

        var deadLetterFlags = new DeadLetterFlags();
        services.AddSingleton(deadLetterFlags);

        var provider = services.BuildServiceProvider();

        var subscriptionsManager = provider.GetRequiredService<IHostedService>();
        var cursorRepository = provider.GetRequiredService<ISubscriptionCursorRepository>();
        var store = provider.GetRequiredService<TestStore>();
        var client = provider.GetRequiredService<EventStoreClient>();
        var deadLetterService = provider.GetRequiredService<DeadLetterManager>();

        await PrepareCategoryStream(client, cancellationToken);

        var subscriberName = TestDeadLetterSubscriber.SubscriberName;
        var categoryStreamName = TestDeadLetterSubscriber.CategoryStreamName;

        var testId = Guid.NewGuid().ToString();
        var streamId = $"deadletter_tests-{testId}";
        IEvent[] unitTestedEvents = [new UnitTested(testId)];

        deadLetterFlags.SetFlag(testId, flag: true);

        //Set cursor to current stream length to avoid full projection
        var categoryStreamLength = await GetCategoryStreamPosition(client, categoryStreamName);
        await cursorRepository.UpsertSubscriptionCursor(subscriberName, categoryStreamName,
            categoryStreamLength);

        await subscriptionsManager.StartAsync(cancellationToken);

        //Act
        await client.AppendToStreamAsync(streamId, StreamState.NoStream, unitTestedEvents,
            cancellationToken);
        await WaitForSubscriptionToCatchup(cursorRepository, subscriberName, categoryStreamName,
            categoryStreamLength + (ulong)unitTestedEvents.Length);

        await subscriptionsManager.StopAsync(cancellationToken);

        //Assert
        var deadLetters = await deadLetterService.GetAllDeadLettersForSubscriber(subscriberName);
        deadLetters.ShouldHaveSingleItem();

        var deadLetter = deadLetters.Single();
        deadLetter.Event.ShouldBeOfType<UnitTested>()
            .ShouldBeEquivalentTo(unitTestedEvents.Single());
        store.Tests.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenADeadLetter_WhenRedelivered_ThenEventIsHandled()
    {
        //Arrange
        var cancellationToken = CancellationToken.None;
        var services = new ServiceCollection();
        services.AddEventStore(Options.KurrentDB, [Assembly.GetAssembly(typeof(UnitTested))!]!)
            .AddSubscriber<TestDeadLetterSubscriber>()
            .AddSingleton<ISubscriptionCursorRepository, InMemoryCursorRepository>()
            .AddDeadLetters(_ => new InMemoryDeadLetterRepository())
            .AddSingleton<TestStore>();

        var deadLetterFlags = new DeadLetterFlags();
        services.AddSingleton(deadLetterFlags);

        var provider = services.BuildServiceProvider();

        var subscriptionsManager = provider.GetRequiredService<IHostedService>();
        var cursorRepository = provider.GetRequiredService<ISubscriptionCursorRepository>();
        var store = provider.GetRequiredService<TestStore>();
        var client = provider.GetRequiredService<EventStoreClient>();
        var deadLetterManager = provider.GetRequiredService<DeadLetterManager>();

        await PrepareCategoryStream(client, cancellationToken);

        var subscriberName = TestDeadLetterSubscriber.SubscriberName;
        var categoryStreamName = TestDeadLetterSubscriber.CategoryStreamName;

        var testId = Guid.NewGuid().ToString();
        var streamId = $"deadletter_tests-{testId}";
        IEvent[] unitTestedEvents = [new UnitTested(testId)];

        deadLetterFlags.SetFlag(testId, flag: true);

        //Set cursor to current stream length to avoid full projection
        var categoryStreamLength = await GetCategoryStreamPosition(client, categoryStreamName);
        await cursorRepository.UpsertSubscriptionCursor(subscriberName, categoryStreamName,
            categoryStreamLength);
        _output.WriteLine($"{categoryStreamLength.ToString()}");

        await subscriptionsManager.StartAsync(cancellationToken);

        await client.AppendToStreamAsync(streamId, StreamState.NoStream, unitTestedEvents,
            cancellationToken);
        await WaitForSubscriptionToCatchup(cursorRepository, subscriberName, categoryStreamName,
            categoryStreamLength + (ulong)unitTestedEvents.Length);

        //Act
        deadLetterFlags.SetFlag(testId, flag: false);

        var deadLetters = await deadLetterManager.GetAllDeadLettersForSubscriber(subscriberName);
        await deadLetterManager.RedeliverDeadLetter(deadLetters.Single().Id);

        await subscriptionsManager.StopAsync(cancellationToken);

        //Assert
        deadLetters = await deadLetterManager.GetAllDeadLettersForSubscriber(subscriberName);
        deadLetters.ShouldBeEmpty();

        store.Tests.ContainsKey(testId).ShouldBeTrue();
        store.Tests[testId].ShouldBe(1);
    }


    [Fact]
    public async Task GivenManyDeadLetters_WhenAllRedelivered_ThenEventsAreHandled()
    {
        //Arrange
        var cancellationToken = CancellationToken.None;
        var services = new ServiceCollection();
        services.AddEventStore(Options.KurrentDB, [Assembly.GetAssembly(typeof(UnitTested))!]!)
            .AddSubscriber<TestDeadLetterSubscriber>()
            .AddSingleton<ISubscriptionCursorRepository, InMemoryCursorRepository>()
            .AddDeadLetters(_ => new InMemoryDeadLetterRepository())
            .AddSingleton<TestStore>();

        var deadLetterFlags = new DeadLetterFlags();
        services.AddSingleton(deadLetterFlags);

        var provider = services.BuildServiceProvider();

        var subscriptionsManager = provider.GetRequiredService<IHostedService>();
        var cursorRepository = provider.GetRequiredService<ISubscriptionCursorRepository>();
        var store = provider.GetRequiredService<TestStore>();
        var client = provider.GetRequiredService<EventStoreClient>();
        var deadLetterManager = provider.GetRequiredService<DeadLetterManager>();

        await PrepareCategoryStream(client, cancellationToken);

        var subscriberName = TestDeadLetterSubscriber.SubscriberName;
        var categoryStreamName = TestDeadLetterSubscriber.CategoryStreamName;

        var testId = Guid.NewGuid().ToString();
        var streamId = $"deadletter_tests-{testId}";
        IEvent[] unitTestedEvents =
            [new UnitTested(testId), new UnitTested(testId), new UnitTested(testId)];

        deadLetterFlags.SetFlag(testId, flag: true);

        //Set cursor to current stream length to avoid full projection
        var categoryStreamLength = await GetCategoryStreamPosition(client, categoryStreamName);
        await cursorRepository.UpsertSubscriptionCursor(subscriberName, categoryStreamName,
            categoryStreamLength);
        _output.WriteLine($"{categoryStreamLength.ToString()}");

        await subscriptionsManager.StartAsync(cancellationToken);

        await client.AppendToStreamAsync(streamId, StreamState.NoStream, unitTestedEvents,
            cancellationToken);
        await WaitForSubscriptionToCatchup(cursorRepository, subscriberName, categoryStreamName,
            categoryStreamLength + (ulong)unitTestedEvents.Length);

        //Act
        deadLetterFlags.SetFlag(testId, flag: false);

        await deadLetterManager.RedeliverAllDeadLettersForSubscriber(subscriberName);
        await subscriptionsManager.StopAsync(cancellationToken);

        //Assert
        var deadLetters = await deadLetterManager.GetAllDeadLettersForSubscriber(subscriberName);
        deadLetters.ShouldBeEmpty();

        store.Tests.ContainsKey(testId).ShouldBeTrue();
        store.Tests[testId].ShouldBe(3);
    }

    private static async Task PrepareCategoryStream(EventStoreClient client,
        CancellationToken cancellationToken)
    {
        if ((await GetCategoryStreamLength(client, "$ce-deadletter_tests")) != 0)
        {
            return;
        }

        // fire off random event so category stream exists
        var streamId = $"deadletter_tests-{Guid.NewGuid().ToString()}";
        await client.AppendToStreamAsync(streamId, StreamState.NoStream, [new UnitTested(streamId)],
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
