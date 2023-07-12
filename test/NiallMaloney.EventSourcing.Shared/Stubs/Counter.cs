using NiallMaloney.EventSourcing.Aggregates;
using NiallMaloney.EventSourcing.Shared.Stubs.Events;

namespace NiallMaloney.EventSourcing.Shared.Stubs;

[Category("counter")]
public class Counter : Aggregate
{
    public int CurrentCount { get; private set; } = 0;

    public Counter()
    {
        When<CountIncreased>(Apply);
        When<CountDecreased>(Apply);
    }

    public void Increase(int amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException();
        }

        var newCount = CurrentCount + amount;
        RaiseEvent(new CountIncreased(newCount, amount));
    }

    public void Decrease(int amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException();
        }

        var newCount = CurrentCount - amount;
        RaiseEvent(new CountDecreased(newCount, amount));
    }

    private void Apply(CountIncreased evnt)
    {
        CurrentCount = evnt.Count;
    }

    private void Apply(CountDecreased evnt)
    {
        CurrentCount = evnt.Count;
    }
}
