namespace NiallMaloney.EventSourcing.Shared.Stubs.Events;

[Event("CountDecreased")]
public record CountDecreased(string CounterId, int Count, int DecreasedBy) : IEvent;
