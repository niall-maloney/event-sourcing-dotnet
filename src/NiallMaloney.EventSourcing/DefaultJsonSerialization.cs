using System.Text.Json;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace NiallMaloney.EventSourcing;

public class DefaultJsonSerialization
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }
        .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
}
