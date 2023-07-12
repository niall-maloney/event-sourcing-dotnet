namespace NiallMaloney.EventSourcing.Shared.Stubs.Events;

[Event("CountIncreased")]
public record CountIncreased(int Count, int IncreasedBy) : IEvent;
