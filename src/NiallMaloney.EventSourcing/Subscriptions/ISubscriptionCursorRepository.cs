namespace NiallMaloney.EventSourcing.Subscriptions;

public interface ISubscriptionCursorRepository
{
    Task<ulong?> GetSubscriptionCursor(string subscriberName, string streamName);

    Task UpsertSubscriptionCursor(string subscriberName, string streamName, ulong position);
}
