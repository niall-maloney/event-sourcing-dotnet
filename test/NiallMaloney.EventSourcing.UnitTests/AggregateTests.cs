using NiallMaloney.EventSourcing.Shared.Stubs;
using NiallMaloney.EventSourcing.Shared.Stubs.Events;
using Shouldly;

namespace NiallMaloney.EventSourcing.UnitTests;

public class AggregateTests
{
    [Fact]
    public void GivenAnAggregate_WhenValidCommands_ThenUnsavedEvents()
    {
        //Arrange
        var counter = new Counter { Id = "cntr-1" };

        //Act
        counter.Increase(10);
        counter.Increase(10);
        counter.Decrease(10);

        //Assert
        counter.CurrentCount.ShouldBe(10);
        counter.Version.ShouldBe(3);
        counter.UnsavedEvents.ShouldBe(new IEvent[]
        {
            new CountIncreased(counter.Id, 10, 10),
            new CountIncreased(counter.Id, 20, 10),
            new CountDecreased(counter.Id, 10, 10)
        });
    }

    [Fact]
    public void GivenAnAggregate_WhenValidCommands_ThenStateUpdatesImmediately()
    {
        //Arrange
        var counter = new Counter { Id = "cntr-1" };

        //Act
        counter.Increase(10);
        var countAfterFirst = counter.CurrentCount;
        var versionAfterFirst = counter.Version;

        counter.Increase(10);
        var countAfterSecond = counter.CurrentCount;
        var versionAfterSecond = counter.Version;

        counter.Decrease(10);
        var countAfterThird = counter.CurrentCount;
        var versionAfterThird = counter.Version;

        //Assert
        countAfterFirst.ShouldBe(10);
        versionAfterFirst.ShouldBe(1);

        countAfterSecond.ShouldBe(20);
        versionAfterSecond.ShouldBe(2);

        countAfterThird.ShouldBe(10);
        versionAfterThird.ShouldBe(3);
    }

    [Fact]
    public void GivenAnAggregate_WhenInvalidCommand_ThenExceptionThrows()
    {
        //Arrange
        var counter = new Counter();

        //Act
        var action = () => counter.Increase(-10);

        //Assert
        Should.Throw<InvalidOperationException>(action);
    }
}
