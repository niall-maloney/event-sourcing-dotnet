namespace NiallMaloney.EventSourcing.Shared.Stubs.Events;

[Event("CountIncreased")]
public record CountIncreased(string CounterId, int Count, int IncreasedBy) : IEvent;
