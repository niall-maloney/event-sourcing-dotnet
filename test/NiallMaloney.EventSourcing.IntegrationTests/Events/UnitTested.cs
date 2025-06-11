namespace NiallMaloney.EventSourcing.IntegrationTests.Events;

[Event("TestEvent")]
public record UnitTested(string TestId) : IEvent;
