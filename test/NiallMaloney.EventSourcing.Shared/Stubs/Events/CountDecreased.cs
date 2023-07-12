namespace NiallMaloney.EventSourcing.Shared.Stubs.Events;

[Event("CountDecreased")]
public record CountDecreased(int Count, int DecreasedBy) : IEvent;
