namespace NiallMaloney.EventSourcing;

public record EventEnvelope<T>(T Event, EventMetadata Metadata) where T : class, IEvent;