using System.Reflection;
using EventStore.Client;
using Shouldly;

namespace NiallMaloney.EventSourcing.IntegrationTests;

// run eventstore-init.sh before running these tests as they rely on an ES instance
public class EventStoreTests
{
    private readonly EventStoreClient _client;

    public EventStoreTests()
    {
        var eventStore =
            new EventStore.Client.EventStoreClient(
                EventStoreClientSettings.Create(Options.EventStore.ConnectionString));
        var serializer = new EventSerializer(Assembly.GetAssembly(typeof(UnitTested))!);
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
        await _client.AppendToStreamAsync(streamId, StreamRevision.None, [unitTested]);

        var enumerable =
            await _client.ReadStreamAsync(streamId, StreamPosition.Start, Direction.Forwards, resolveLinkTos: true);

        //Assert
        var events = await enumerable!.ToListAsync();
        events.Count.ShouldBe(1);

        var envelope = events.Single();

        var evnt = envelope.Event as UnitTested;
        evnt.ShouldBeOfType<UnitTested>();
        evnt.TestId.ShouldBe(testId);

        var metadata = envelope.Metadata;
        metadata.EventId.ShouldBeOfType<Uuid>();
        metadata.StreamPosition.ShouldBe(0UL);
        metadata.AggregatedStreamPosition.ShouldBe(0UL);
    }

    [Fact]
    public async Task CanReadAStreamFromBeginning()
    {
        //Arrange
        var streamId = Guid.NewGuid().ToString();
        var testId = Guid.NewGuid().ToString();
        UnitTested[] unitTestedEvents = [new(testId), new(testId), new(testId)];

        //Act
        await _client.AppendToStreamAsync(streamId, StreamRevision.None, unitTestedEvents);

        var enumerable = await _client.ReadStreamFromBeginningAsync(streamId, resolveLinkTos: true);

        //Assert
        var events = await enumerable!.ToListAsync();
        events.Count.ShouldBe(3);

        var position = 0UL;
        foreach (var envelope in events)
        {
            var evnt = envelope.Event as UnitTested;
            evnt.ShouldBeOfType<UnitTested>();
            evnt.TestId.ShouldBe(testId);

            var metadata = envelope.Metadata;
            metadata.EventId.ShouldBeOfType<Uuid>();
            metadata.StreamPosition.ShouldBe(position);
            metadata.AggregatedStreamPosition.ShouldBe(position);

            position++;
        }
    }
}

[Event("TestEvent")]
public record UnitTested(string TestId) : IEvent;
