using MeshSim.Engine;
using MeshSim.Engine.IO;

namespace MeshSim.Tests;

public class RoutingStrategyRegistryTests
{
    [Theory]
    [InlineData("Dijkstra", true)]
    [InlineData("dijkstra", true)]
    [InlineData("DIJKSTRA", true)]
    [InlineData("OSPF", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsKnownMatchesRegisteredNamesIgnoringCase(string? name, bool expected)
    {
        Assert.Equal(expected, RoutingStrategyRegistry.IsKnown(name));
    }

    [Fact]
    public void NamesContainDijkstra()
    {
        Assert.Contains("Dijkstra", RoutingStrategyRegistry.Names);
    }

    [Theory]
    [InlineData("Dijkstra")]
    [InlineData("dijkstra")]
    public void CreateReturnsDijkstraStrategy(string name)
    {
        IRoutingStrategy strategy = RoutingStrategyRegistry.Create(name);

        Assert.IsType<DijkstraRoutingStrategy>(strategy);
    }

    [Fact]
    public void CreateReturnsNewInstanceOnEveryCall()
    {
        IRoutingStrategy first = RoutingStrategyRegistry.Create("Dijkstra");
        IRoutingStrategy second = RoutingStrategyRegistry.Create("Dijkstra");

        Assert.NotSame(first, second);
    }

    [Fact]
    public void CreateThrowsArgumentExceptionForUnknownStrategy()
    {
        Assert.Throws<ArgumentException>(() => RoutingStrategyRegistry.Create("OSPF"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateThrowsForNullOrBlankName(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => RoutingStrategyRegistry.Create(name!));
    }
}