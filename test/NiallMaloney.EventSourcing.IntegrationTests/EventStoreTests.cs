using System.Reflection;
using EventStore.Client;
using FluentAssertions;

namespace NiallMaloney.EventSourcing.IntegrationTests;

// run eventstore-init.sh before running these tests as they rely on an ES instance
public class EventStoreTests
{
    private readonly EventStoreClient _client;

    private readonly EventStoreClientOptions _eventStoreOptions =
        new("esdb+discover://localhost:2113?tls=false&keepAliveTimeout=10000&keepAliveInterval=10000");

    public EventStoreTests()
    {
        var eventStore =
            new EventStore.Client.EventStoreClient(
                EventStoreClientSettings.Create(_eventStoreOptions.ConnectionString));
        var serializer = new EventSerializer(Assembly.GetAssembly(typeof(UnitTested)));
        _client = new EventStoreClient(eventStore, serializer);
    }

    [Fact]
    public async Task CanWriteAndReadAnEvent()
    {
        //Arrange
        var streamId = Guid.NewGuid().ToString();
        var testId = Guid.NewGuid().ToString();
        var unitTested = new UnitTested(testId);

        //Act
        await _client.AppendToStreamAsync(streamId, StreamRevision.None, new[] { unitTested });

        var enumerable =
            await _client.ReadStreamAsync(streamId, StreamPosition.Start, Direction.Forwards, resolveLinkTos: true);

        //Assert
        var events = await enumerable.ToListAsync();
        events.Count.Should().Be(1);

        var envelope = events.Single();

        var evnt = envelope.Event.Should().BeOfType<UnitTested>().Subject;
        evnt.TestId.Should().Be(testId);

        var metadata = envelope.Metadata;
        metadata.EventId.Should().NotBeNull();
        metadata.StreamPosition.Should().Be(0);
        metadata.AggregatedStreamPosition.Should().Be(0);
    }

    [Fact]
    public async Task CanReadAStreamFromBeginning()
    {
        //Arrange
        var streamId = Guid.NewGuid().ToString();
        var testId = Guid.NewGuid().ToString();
        var unitTestedEvents = new[] { new UnitTested(testId), new UnitTested(testId), new UnitTested(testId) };

        //Act
        await _client.AppendToStreamAsync(streamId, StreamRevision.None, unitTestedEvents);

        var enumerable = await _client.ReadStreamFromBeginningAsync(streamId, resolveLinkTos: true);

        //Assert
        var events = await enumerable.ToListAsync();
        events.Count.Should().Be(3);

        var position = 0UL;
        foreach (var envelope in events)
        {
            var evnt = envelope.Event.Should().BeOfType<UnitTested>().Subject;
            evnt.TestId.Should().Be(testId);

            var metadata = envelope.Metadata;
            metadata.EventId.Should().NotBeNull();
            metadata.StreamPosition.Should().Be(position);
            metadata.AggregatedStreamPosition.Should().Be(position);

            position++;
        }
    }
}

[Event("TestEvent")]
public record UnitTested(string TestId) : IEvent;
