using NodaTime;

namespace NiallMaloney.EventSourcing;

public record EventMetadata(Instant OccurredAt, ulong StreamPosition, ulong CategoryStreamPosition);
