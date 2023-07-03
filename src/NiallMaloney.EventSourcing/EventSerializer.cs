using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using EventStore.Client;

namespace NiallMaloney.EventSourcing;

public class EventSerializer
{
    private readonly ImmutableDictionary<string, Type> _eventTypeLookup;

    public EventSerializer(params Assembly[] assemblies)
    {
        _eventTypeLookup = GetEventTypeLookup(assemblies);
    }

    public byte[] Serialize(IEvent evnt)
    {
        var eventType = IEvent.GetEventType(evnt.GetType());

        if (!_eventTypeLookup.ContainsKey(eventType))
        {
            throw new InvalidOperationException($"Could not find Event class for {eventType}.");
        }

        return JsonSerializer.SerializeToUtf8Bytes(evnt, evnt.GetType(), DefaultJsonSerializerOptions.Options);
    }

    public IEvent Deserialize(EventRecord eventRecord)
    {
        if (!_eventTypeLookup.TryGetValue(eventRecord.EventType, out var eventType))
        {
            throw new InvalidOperationException($"Could not find Event class for {eventRecord.EventType}.");
        }

        var deserializedEvent = JsonSerializer.Deserialize(
            eventRecord.Data.Span,
            eventType,
            DefaultJsonSerializerOptions.Options);

        if (deserializedEvent is null)
        {
            throw new InvalidOperationException($"Could not deserialize event data to {eventType.Name}.");
        }

        return (IEvent)deserializedEvent;
    }

    private static ImmutableDictionary<string, Type> GetEventTypeLookup(IEnumerable<Assembly> assemblies)
    {
        var eventSubclasses = assemblies.SelectMany(
            assembly => assembly.DefinedTypes.Where(type => type.ImplementedInterfaces.Contains(typeof(IEvent))));

        return eventSubclasses.ToImmutableDictionary(IEvent.GetEventType, type => type.AsType());
    }
}
