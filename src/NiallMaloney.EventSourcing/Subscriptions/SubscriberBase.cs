namespace NiallMaloney.EventSourcing.Subscriptions;

public class SubscriberBase : EventHandler, ISubscriber
{
    public async Task<EventHandlerResult> Handle(EventEnvelope<IEvent> envelope)
    {
        if (!CanHandle(envelope))
        {
            return new EventHandlerResult(Success: true);
        }

        return await base.Handle(envelope);
    }
}
