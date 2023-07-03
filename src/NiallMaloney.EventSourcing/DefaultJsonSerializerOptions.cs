using System.Text.Json;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace NiallMaloney.EventSourcing;

public static class DefaultJsonSerializerOptions
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }
        .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
}
