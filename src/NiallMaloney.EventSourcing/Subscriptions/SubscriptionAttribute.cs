namespace NiallMaloney.EventSourcing.Subscriptions;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SubscriptionAttribute : Attribute
{
    public string StreamName { get; }

    public CursorFromStream Begin { get; }

    public SubscriptionAttribute(string streamName, CursorFromStream begin = CursorFromStream.Start)
    {
        StreamName = streamName;
        Begin = begin;
    }
}
