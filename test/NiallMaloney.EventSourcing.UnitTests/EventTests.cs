using EventStore.Client;
using NodaTime;
using Shouldly;

namespace NiallMaloney.EventSourcing.UnitTests;

// TODO delete these tests
public class EventTests
{
    [Fact]
    public void CanCreateEventEnvelope()
    {
        //Arrange
        var testId = NewGuidString();
        var evntId = Uuid.NewUuid();
        var occuredAt = Instant.FromUtc(2023, 07, 01, 12, 00, 00);
        ulong streamPosition = 1;
        ulong categoryStreamPosition = 1;
        var metadata = new EventMetadata(evntId, occuredAt, streamPosition, categoryStreamPosition);

        //Act
        UnitTested evnt = new(testId);
        EventEnvelope<UnitTested> envelope = new(evnt, metadata);

        //Assert
        envelope.Event.ShouldBe(evnt);
        envelope.Metadata.ShouldBe(metadata);

        envelope.Event.TestId.ShouldBe(testId);

        envelope.Metadata.EventId.ShouldBe(evntId);
        envelope.Metadata.OccurredAt.ShouldBe(occuredAt);
        envelope.Metadata.StreamPosition.ShouldBe(streamPosition);
        envelope.Metadata.AggregatedStreamPosition.ShouldBe(categoryStreamPosition);
    }

    private static string NewGuidString() => Guid.NewGuid().ToString();
}

[Event("TestEvent")]
public record UnitTested(string TestId) : IEvent;
