namespace NiallMaloney.EventSourcing.UnitTests.Stubs.Events;

[Event("CountDecreased")]
public record CountDecreased(int Count, int DecreasedBy) : IEvent;
