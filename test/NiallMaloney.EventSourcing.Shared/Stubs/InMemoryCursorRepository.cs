using NiallMaloney.EventSourcing.Subscriptions;

namespace NiallMaloney.EventSourcing.Shared.Stubs;

public class InMemoryCursorRepository : ISubscriptionCursorRepository
{
    private readonly IDictionary<string, ulong?> _cursors = new Dictionary<string, ulong?>();

    private string GetCursorKey(string subscriberName, string streamName) => $"{subscriberName}-{streamName}";

    public Task<ulong?> GetSubscriptionCursor(string subscriberName, string streamName)
    {
        var cursorKey = GetCursorKey(subscriberName, streamName);
        if (!_cursors.ContainsKey(cursorKey))
        {
            return Task.FromResult((ulong?)null);
        }
        return Task.FromResult(_cursors[cursorKey]);
    }

    public Task UpsertSubscriptionCursor(string subscriberName, string streamName, ulong position)
    {
        _cursors[GetCursorKey(subscriberName, streamName)] = position;
        return Task.CompletedTask;
    }
}
