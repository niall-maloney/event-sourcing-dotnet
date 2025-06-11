using NodaTime;

namespace NiallMaloney.EventSourcing.DeadLetters;

public record EventDeadLetter(
    string Id,
    string SubscriberName,
    string SubscriptionStreamId,
    ulong SubscriptionEventNumber,
    object Event,
    EventMetadata Metadata,
    Instant DeadLetteredAt);
