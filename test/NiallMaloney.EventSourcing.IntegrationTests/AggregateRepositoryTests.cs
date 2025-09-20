using System.Reflection;
using EventStore.Client;
using KurrentDB.Client;
using Microsoft.Extensions.DependencyInjection;
using NiallMaloney.EventSourcing.Aggregates;
using NiallMaloney.EventSourcing.Shared.Stubs;
using NiallMaloney.EventSourcing.Shared.Stubs.Events;
using Shouldly;

namespace NiallMaloney.EventSourcing.IntegrationTests;

// run eventstore-init.sh before running these tests as they rely on an ES instance
public class AggregateRepositoryTests
{
    private readonly EventStoreClient _client;
    private readonly AggregateRepository _repository;

    public AggregateRepositoryTests()
    {
        var services = new ServiceCollection();
        services.AddEventStore(Options.KurrentDB, [Assembly.GetAssembly(typeof(CountDecreased))!]);

        var provider = services.BuildServiceProvider();
        _client = provider.GetRequiredService<EventStoreClient>();
        _repository = provider.GetRequiredService<AggregateRepository>();
    }

    [Fact]
    public async Task GivenNoEvents_WhenLoadAggregate_ThenInitialAggregate()
    {
        //Arrange
        var counterId = NewGuidString();

        //Act
        var counter = await _repository.LoadAggregate<Counter>(counterId);

        //Assert

        counter.ShouldSatisfyAllConditions(
            () => counter.ShouldNotBeNull(),
            () => counter.ShouldBeOfType<Counter>(),
            () => counter.Id.ShouldBe(counterId),
            () => counter.Version.ShouldBe(0),
            () => counter.CurrentCount.ShouldBe(0));
    }

    [Fact]
    public async Task GivenSavedEvents_WhenLoadAggregate_ThenAggregateStateExpected()
    {
        //Arrange
        var counterId = NewGuidString();

        var events = new IEvent[]
        {
            new CountIncreased(counterId, 20, 20),
            new CountDecreased(counterId, 10, 10)
        };
        await _client.AppendToStreamAsync($"counters-{counterId}", StreamState.NoStream, events);

        //Act
        var counter = await _repository.LoadAggregate<Counter>(counterId);

        //Assert

        counter.ShouldSatisfyAllConditions(
            () => counter.ShouldNotBeNull(),
            () => counter.ShouldBeOfType<Counter>(),
            () => counter.Id.ShouldBe(counterId),
            () => counter.Version.ShouldBe(2),
            () => counter.CurrentCount.ShouldBe(10));
    }

    [Fact]
    public async Task GivenUnsavedEvents_WhenSaveAggregate_ThenEventsSaved()
    {
        //Arrange
        var counterId = NewGuidString();

        var counter = new Counter { Id = counterId };

        //Act
        counter.Increase(20);
        counter.Decrease(10);

        await _repository.SaveAggregate(counter);

        counter = await _repository.LoadAggregate<Counter>(counterId);

        //Assert
        counter.ShouldSatisfyAllConditions(
            () => counter.ShouldNotBeNull(),
            () => counter.ShouldBeOfType<Counter>(),
            () => counter.Id.ShouldBe(counterId),
            () => counter.Version.ShouldBe(2),
            () => counter.CurrentCount.ShouldBe(10),
            () => counter.UnsavedEvents.ShouldBeEmpty());
    }

    private static string NewGuidString() => Guid.NewGuid().ToString();
}
