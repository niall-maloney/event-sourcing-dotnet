using System.Reflection;
using EventStore.Client;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using NiallMaloney.EventSourcing.Aggregates;
using NiallMaloney.EventSourcing.Shared.Stubs;
using NiallMaloney.EventSourcing.Shared.Stubs.Events;
using NiallMaloney.EventSourcing.Subscriptions;

namespace NiallMaloney.EventSourcing.IntegrationTests;

// run eventstore-init.sh before running these tests as they rely on an ES instance
public class AggregateRepositoryTests
{
    private readonly EventStoreClient _client;
    private readonly AggregateRepository _repository;

    public AggregateRepositoryTests()
    {
        var services = new ServiceCollection();
        services.AddEventStore(Options.EventStore, new[] { Assembly.GetAssembly(typeof(CountDecreased)) }!);

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
        using var _ = new AssertionScope();
        counter.Should().NotBeNull();
        counter.Should().BeOfType<Counter>();
        counter.Id.Should().Be(counterId);
        counter.Version.Should().Be(0);
        counter.CurrentCount.Should().Be(0);
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
        await _client.AppendToStreamAsync($"counters-{counterId}", StreamRevision.None, events);

        //Act
        var counter = await _repository.LoadAggregate<Counter>(counterId);

        //Assert
        using var _ = new AssertionScope();
        counter.Should().NotBeNull();
        counter.Should().BeOfType<Counter>();
        counter.Id.Should().Be(counterId);
        counter.Version.Should().Be(2);
        counter.CurrentCount.Should().Be(10);
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
        using var _ = new AssertionScope();
        counter.Should().NotBeNull();
        counter.Should().BeOfType<Counter>();
        counter.Id.Should().Be(counterId);
        counter.Version.Should().Be(2);
        counter.CurrentCount.Should().Be(10);
        counter.UnsavedEvents.Should().BeEmpty();
    }

    private static string NewGuidString() => Guid.NewGuid().ToString();
}
