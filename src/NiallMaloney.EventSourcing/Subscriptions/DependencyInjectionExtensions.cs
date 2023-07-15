using Microsoft.Extensions.DependencyInjection;

namespace NiallMaloney.EventSourcing.Subscriptions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSubscriber<T>(this IServiceCollection services) where T : class, ISubscriber =>
        services.AddTransient<ISubscriber, T>();

    public static IServiceCollection AddSubscriptions(this IServiceCollection services) =>
        services.AddHostedService<SubscriptionsManager>();
}
