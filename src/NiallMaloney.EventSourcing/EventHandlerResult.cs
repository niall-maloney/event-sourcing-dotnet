namespace NiallMaloney.EventSourcing;

public record EventHandlerResult(bool Success, Exception? Exception = null);