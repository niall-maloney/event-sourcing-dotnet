using FluentAssertions;

namespace NiallMaloney.EventSourcing.UnitTests;

public class SkeletonTests
{
    [Fact]
    public void IsTrue()
    {
        const bool isTrue = true;
        isTrue.Should().BeTrue();
    }
}
