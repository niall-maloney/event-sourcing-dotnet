using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NiallMaloney.EventSourcing.Aggregates;
using NiallMaloney.EventSourcing.Subscriptions;

namespace NiallMaloney.EventSourcing;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddEventStore(
        this IServiceCollection services,
        KurrentDBClientOptions kurrentDBClientOptions,
        Assembly[] assemblies) =>
        services.AddKurrentDBClient(kurrentDBClientOptions.ConnectionString)
            .AddTransient<EventStoreClient>()
            .AddSingleton(_ => new EventSerializer(assemblies))
            .AddAggregateRepository()
            .AddSubscriptions();
}
