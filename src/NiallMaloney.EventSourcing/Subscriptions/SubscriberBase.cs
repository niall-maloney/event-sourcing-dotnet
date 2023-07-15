namespace NiallMaloney.EventSourcing.Subscriptions;

public class SubscriberBase : EventHandler, ISubscriber
{
    public async Task Handle(EventEnvelope<IEvent> envelope)
    {
        if (!CanHandle(envelope))
        {
            return;
        }

        await base.Handle(envelope);
    }
}