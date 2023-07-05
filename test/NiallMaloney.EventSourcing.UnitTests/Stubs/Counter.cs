using NiallMaloney.EventSourcing.Aggregates;
using NiallMaloney.EventSourcing.UnitTests.Stubs.Events;

namespace NiallMaloney.EventSourcing.UnitTests.Stubs;

public class Counter : Aggregate
{
    private int _currentCount = 0;

    public int CurrentCount => _currentCount;

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

        var newCount = _currentCount + amount;
        RaiseEvent(new CountIncreased(newCount, amount));
    }

    public void Decrease(int amount)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException();
        }

        var newCount = _currentCount - amount;
        RaiseEvent(new CountDecreased(newCount, amount));
    }

    private void Apply(CountIncreased evnt)
    {
        _currentCount = evnt.Count;
    }

    private void Apply(CountDecreased evnt)
    {
        _currentCount = evnt.Count;
    }
}
