using FluentAssertions;
using NiallMaloney.EventSourcing.Shared.Stubs;
using NiallMaloney.EventSourcing.Shared.Stubs.Events;

namespace NiallMaloney.EventSourcing.UnitTests;

public class AggregateTests
{
    [Fact]
    public void GivenAnAggregate_WhenValidCommands_ThenUnsavedEvents()
    {
        //Arrange
        var counter = new Counter();

        //Act
        counter.Increase(10);
        counter.Increase(10);
        counter.Decrease(10);

        //Assert
        counter.CurrentCount.Should().Be(10);
        counter.Version.Should().Be(3);
        counter.UnsavedEvents.Should().BeEquivalentTo(new IEvent[]
        {
            new CountIncreased(10, 10),
            new CountIncreased(20, 10),
            new CountDecreased(10, 10)
        }, options => options.RespectingRuntimeTypes());
    }

    [Fact]
    public void GivenAnAggregate_WhenValidCommands_ThenStateUpdatesImmediately()
    {
        //Arrange
        var counter = new Counter();

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
        countAfterFirst.Should().Be(10);
        versionAfterFirst.Should().Be(1);

        countAfterSecond.Should().Be(20);
        versionAfterSecond.Should().Be(2);

        countAfterThird.Should().Be(10);
        versionAfterThird.Should().Be(3);
    }

    [Fact]
    public void GivenAnAggregate_WhenInvalidCommand_ThenExceptionThrows()
    {
        //Arrange
        var counter = new Counter();

        //Act
        var action = () => counter.Increase(-10);

        //Assert
        action.Should().Throw<InvalidOperationException>();
    }
}
