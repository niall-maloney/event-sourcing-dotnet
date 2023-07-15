using NiallMaloney.EventSourcing.Aggregates;
using NiallMaloney.EventSourcing.Shared.Stubs.Events;

namespace NiallMaloney.EventSourcing.Shared.Stubs;

[Category("counters")]
public class Counter : Aggregate
{
    public int CurrentCount { get; private set; }

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
        RaiseEvent(new CountIncreased(Id, newCount, amount));
    }

    public void Decrease(int amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException();
        }

        var newCount = CurrentCount - amount;
        RaiseEvent(new CountDecreased(Id, newCount, amount));
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
