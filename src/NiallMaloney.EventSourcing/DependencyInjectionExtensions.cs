using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NiallMaloney.EventSourcing;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddEventStore(
        this IServiceCollection services,
        EventStoreClientOptions eventStoreClientOptions,
        Assembly[] assemblies) =>
        services.AddEventStoreClient(eventStoreClientOptions.ConnectionString)
            .AddTransient<EventStoreClient>()
            .AddSingleton(_ => new EventSerializer(assemblies));
}
