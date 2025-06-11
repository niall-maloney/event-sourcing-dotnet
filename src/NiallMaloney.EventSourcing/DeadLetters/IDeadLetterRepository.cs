namespace NiallMaloney.EventSourcing.DeadLetters;

public interface IDeadLetterRepository
{
    public Task InsertDeadLetter(EventDeadLetter deadLetter);

    public Task<EventDeadLetter?> GetDeadLetter(string deadLetterId);

    public Task<IReadOnlyList<EventDeadLetter>> GetAllDeadLettersForSubscriber(string subscriberName);

    public Task DeleteDeadLetter(string deadLetterId);
}
