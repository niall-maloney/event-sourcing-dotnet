using NodaTime;

namespace NiallMaloney.EventSourcing;

public record EventMetadata(
    string EventId,
    Instant OccurredAt,
    ulong StreamPosition,
    ulong CategoryStreamPosition);
