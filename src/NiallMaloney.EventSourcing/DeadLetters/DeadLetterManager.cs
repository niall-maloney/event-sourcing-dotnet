using NiallMaloney.EventSourcing.Subscriptions;
using NiallMaloney.EventSourcing.Utils;

namespace NiallMaloney.EventSourcing.DeadLetters;

public class DeadLetterManager
{
    private readonly IDeadLetterRepository _repository;
    private readonly IEnumerable<ISubscriber> _subscribers;

    public DeadLetterManager(IDeadLetterRepository repository, IEnumerable<ISubscriber> subscribers)
    {
        _repository = repository;
        _subscribers = subscribers;
    }

    public Task StoreDeadLetter(EventDeadLetter deadLetter) =>
        _repository.InsertDeadLetter(deadLetter);

    public Task<EventDeadLetter?> GetDeadLetter(string deadLetterId) =>
        _repository.GetDeadLetter(deadLetterId);

    public Task<IReadOnlyList<EventDeadLetter>> GetAllDeadLettersForSubscriber(
        string subscriberName) =>
        _repository.GetAllDeadLettersForSubscriber(subscriberName);

    public async Task RedeliverDeadLetter(string deadLetterId)
    {
        var deadLetter = await GetDeadLetter(deadLetterId);
        if (deadLetter == null)
        {
            return;
        }

        foreach (var subscriber in GetSubscribers(deadLetter))
        {
            var evnt = (IEvent)deadLetter.Event;
            var metadata = deadLetter.Metadata;
            var result = await subscriber.Handle(new EventEnvelope<IEvent>(evnt, metadata));
            if (result.Success)
            {
                await _repository.DeleteDeadLetter(deadLetterId);
            }
        }
    }

    public async Task RedeliverAllDeadLettersForSubscriber(string subscriberName)
    {
        var deadLetters = await GetAllDeadLettersForSubscriber(subscriberName);
        foreach (var deadLetter in deadLetters)
        {
            await RedeliverDeadLetter(deadLetter.Id);
        }
    }

    public static string NewId(string subscriberName, string streamName, string eventNumber) =>
        DeterministicIdFactory.NewId(subscriberName, streamName, eventNumber);

    private IList<ISubscriber> GetSubscribers(EventDeadLetter deadLetter) => _subscribers
        .Where(s => ISubscriber.GetSubscriberName(s.GetType()) == deadLetter.SubscriberName)
        .ToList();
}
