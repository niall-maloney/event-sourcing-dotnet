using System.Reflection;

namespace NiallMaloney.EventSourcing.Subscriptions;

public interface ISubscriber
{
    Task Handle(EventEnvelope<IEvent> envelope);

    public static IEnumerable<SubscriptionAttribute> GetSubscriptionAttributes<T>() where T : class =>
        GetSubscriptionAttributes(typeof(T));

    public static IEnumerable<SubscriptionAttribute> GetSubscriptionAttributes(Type type) =>
        type.GetCustomAttributes<SubscriptionAttribute>();

    public static string GetSubscriberName<T>() => GetSubscriberName(typeof(T));

    public static string GetSubscriberName(Type type) =>
        (type.GetCustomAttribute<SubscriberNameAttribute>() ??
         throw new InvalidOperationException($"Missing \"SubscriptionAttribute\" on {type.Name}.")).Name;
}
