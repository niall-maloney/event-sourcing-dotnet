using FluentAssertions;
using NodaTime;

namespace NiallMaloney.EventSourcing.UnitTests;

// TODO delete these tests
public class EventTests
{
    [Fact]
    public void CanCreateEventEnvelope()
    {
        //Arrange
        var testId = NewGuidString();
        var evntId = NewGuidString();
        var occuredAt = Instant.FromUtc(2023, 07, 01, 12, 00, 00);
        ulong streamPosition = 1;
        ulong categoryStreamPosition = 1;
        var metadata = new EventMetadata(evntId, occuredAt, streamPosition, categoryStreamPosition);

        //Act
        UnitTested evnt = new(testId);
        EventEnvelope<UnitTested> envelope = new(evnt, metadata);

        //Assert
        envelope.Event.Should().Be(evnt);
        envelope.Metadata.Should().Be(metadata);

        envelope.Event.TestId.Should().Be(testId);

        envelope.Metadata.EventId.Should().Be(evntId);
        envelope.Metadata.OccurredAt.Should().Be(occuredAt);
        envelope.Metadata.StreamPosition.Should().Be(streamPosition);
        envelope.Metadata.CategoryStreamPosition.Should().Be(categoryStreamPosition);
    }

    private static string NewGuidString() => Guid.NewGuid().ToString();
}

[Event("TestEvent")]
public record UnitTested(string TestId) : IEvent;
