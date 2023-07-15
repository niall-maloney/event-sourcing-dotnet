using Microsoft.Extensions.DependencyInjection;

namespace NiallMaloney.EventSourcing.Aggregates;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddAggregateRepository(this IServiceCollection services) =>
        services.AddTransient<AggregateRepository>();
}
