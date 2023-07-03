using System.Reflection;

namespace NiallMaloney.EventSourcing;

public interface IEvent
{
    public static string GetEventType<T>() where T : class => GetEventType(typeof(T));

    public static string GetEventType(Type type)
    {
        var attribute = type.GetCustomAttribute<EventAttribute>() ??
                        throw new InvalidOperationException($"Missing \"EventAttribute\" on {type.Name}.");
        return attribute.EventType;
    }
}
