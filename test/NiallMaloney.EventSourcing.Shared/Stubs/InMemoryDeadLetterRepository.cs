using System.Collections.Concurrent;
using NiallMaloney.EventSourcing.DeadLetters;

namespace NiallMaloney.EventSourcing.Shared.Stubs;

public class InMemoryDeadLetterRepository : IDeadLetterRepository
{
    private readonly ConcurrentDictionary<string, EventDeadLetter> _deadLetters = new();

    public Task InsertDeadLetter(EventDeadLetter deadLetter)
    {
        _deadLetters.TryAdd(deadLetter.Id, deadLetter);
        return Task.CompletedTask;
    }

    public Task<EventDeadLetter?> GetDeadLetter(string deadLetterId)
    {
        if (_deadLetters.TryGetValue(deadLetterId, out var deadLetter))
        {
            return Task.FromResult<EventDeadLetter?>(deadLetter);
        }

        return Task.FromResult<EventDeadLetter?>(null);
    }

    public Task<IReadOnlyList<EventDeadLetter>> GetAllDeadLettersForSubscriber(string subscriberName)
    {
        return Task.FromResult<IReadOnlyList<EventDeadLetter>>(_deadLetters.Values
            .Where(d => d.SubscriberName == subscriberName).ToList());
    }

    public Task DeleteDeadLetter(string deadLetterId)
    {
        _deadLetters.TryRemove(deadLetterId, out _);
        return Task.CompletedTask;
    }
}
