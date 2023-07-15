namespace NiallMaloney.EventSourcing.Subscriptions;

[AttributeUsage(AttributeTargets.Class)]
public class SubscriberNameAttribute : Attribute
{
    public string Name { get; }

    public SubscriberNameAttribute(string name) => Name = name;
}
