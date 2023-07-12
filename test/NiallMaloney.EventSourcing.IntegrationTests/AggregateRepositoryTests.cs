using System.Reflection;
using EventStore.Client;
using FluentAssertions;
using FluentAssertions.Execution;
using NiallMaloney.EventSourcing.Aggregates;
using NiallMaloney.EventSourcing.Shared.Stubs;
using NiallMaloney.EventSourcing.Shared.Stubs.Events;

namespace NiallMaloney.EventSourcing.IntegrationTests;

// run eventstore-init.sh before running these tests as they rely on an ES instance
public class AggregateRepositoryTests
{
    private readonly EventStoreClient _client;
    private readonly AggregateRepository _repository;

    private readonly EventStoreClientOptions _eventStoreOptions =
        new("esdb+discover://localhost:2113?tls=false&keepAliveTimeout=10000&keepAliveInterval=10000");

    public AggregateRepositoryTests()
    {
        var eventStore =
            new EventStore.Client.EventStoreClient(
                EventStoreClientSettings.Create(_eventStoreOptions.ConnectionString));
        var serializer = new EventSerializer(Assembly.GetAssembly(typeof(CountDecreased)));
        _client = new EventStoreClient(eventStore, serializer);
        _repository = new AggregateRepository(_client);
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
            new CountIncreased(20, 20),
            new CountDecreased(10, 10)
        };
        await _client.AppendToStreamAsync($"counter-{counterId}", StreamRevision.None, events);

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
