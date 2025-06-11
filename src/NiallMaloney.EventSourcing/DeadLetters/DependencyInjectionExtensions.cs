using Microsoft.Extensions.DependencyInjection;

namespace NiallMaloney.EventSourcing.DeadLetters;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddDeadLetters(this IServiceCollection services,
        Func<IServiceProvider, IDeadLetterRepository> implementationFactory) =>
        services.AddSingleton<DeadLetterManager>().AddSingleton(implementationFactory);
}
