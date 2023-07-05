namespace NiallMaloney.EventSourcing.UnitTests.Stubs.Events;

[Event("CountIncreased")]
public record CountIncreased(int Count, int IncreasedBy) : IEvent;
