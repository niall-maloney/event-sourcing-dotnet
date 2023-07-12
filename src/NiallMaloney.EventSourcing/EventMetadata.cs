using EventStore.Client;
using NodaTime;
using NodaTime.Extensions;

namespace NiallMaloney.EventSourcing;

public record EventMetadata(Uuid EventId, Instant OccurredAt, ulong StreamPosition, ulong AggregatedStreamPosition)
{
    public static EventMetadata FromResolvedEvent(ResolvedEvent resolvedEvent) =>
        new(resolvedEvent.Event.EventId, resolvedEvent.Event.Created.ToInstant(), resolvedEvent.Event.EventNumber,
            resolvedEvent.OriginalEventNumber);
}