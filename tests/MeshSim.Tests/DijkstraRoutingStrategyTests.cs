using MeshSim.Core;
using MeshSim.Engine;
using Xunit;

namespace MeshSim.Tests;

public class DijkstraRoutingStrategyEdgeCaseTests
{
    private readonly DijkstraRoutingStrategy _router = new();

    private static MeshNode Node(string id, double x, double y, double range = 120.0, double battery = 100.0)
        => new(id, new Position(x, y), range, battery);

    // ---------- Валідація аргументів ----------

    [Fact]
    public void FindRouteThrowsArgumentNullExceptionWhenTopologyIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _router.FindRoute(null!, "A", "B"));
    }

    [Theory]
    [InlineData("   ", "B")]
    [InlineData("A", "")]
    [InlineData(null, "B")]
    [InlineData("A", null)]
    public void FindRouteThrowsArgumentExceptionForWhitespaceOrNullIds(string? src, string? dst)
    {
        var topology = new NetworkTopology();

        Assert.Throws<ArgumentException>(() => _router.FindRoute(topology, src!, dst!));
    }

    // ---------- Відсутні / неактивні вузли ----------

    [Fact]
    public void FindRouteReturnsEmptyWhenSourceIsUnknown()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("B", 0, 0));

        Assert.Empty(_router.FindRoute(topology, "UNKNOWN", "B"));
    }

    [Fact]
    public void FindRouteTreatsNodeIdsAsCaseSensitive()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 50, 0));

        Assert.Empty(_router.FindRoute(topology, "a", "B"));
        Assert.Empty(_router.FindRoute(topology, "A", "b"));
    }

    [Fact]
    public void FindRouteReturnsEmptyWhenDestinationBatteryIsDepleted()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 50, 0, battery: 0.0));

        Assert.Empty(_router.FindRoute(topology, "A", "B"));
    }

    [Fact]
    public void FindRouteReturnsEmptyWhenSourceEqualsDestinationButNodeIsDepleted()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, battery: 0.0));

        Assert.Empty(_router.FindRoute(topology, "A", "A"));
    }

    [Fact]
    public void FindRouteReturnsEmptyWhenSourceEqualsUnknownNode()
    {
        var topology = new NetworkTopology();

        Assert.Empty(_router.FindRoute(topology, "GHOST", "GHOST"));
    }

    // ---------- Базова структура маршрутів ----------

    [Fact]
    public void FindRouteBuildsMultiHopChainWhenOnlyNeighborsAreInRange()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 100.0));
        topology.AddNode(Node("B", 80, 0, range: 100.0));
        topology.AddNode(Node("C", 160, 0, range: 100.0));
        topology.AddNode(Node("D", 240, 0, range: 100.0));

        IReadOnlyList<string> route = _router.FindRoute(topology, "A", "D");

        Assert.Equal(["A", "B", "C", "D"], route);
    }

    [Fact]
    public void FindRoutePrefersDirectLinkWhenRelayWouldBeLonger()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 200.0));
        topology.AddNode(Node("B", 50, 10, range: 200.0));
        topology.AddNode(Node("C", 100, 0, range: 200.0));

        // Пряма: 100. Через B: 2 * sqrt(50^2 + 10^2) ≈ 101.98.
        Assert.Equal(["A", "C"], _router.FindRoute(topology, "A", "C"));
    }

    [Fact]
    public void FindRouteReturnsPathWithoutRepeatedNodesAndCorrectEndpoints()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 60.0));
        topology.AddNode(Node("B", 50, 0, range: 60.0));
        topology.AddNode(Node("C", 100, 0, range: 60.0));
        topology.AddNode(Node("D", 50, 50, range: 60.0));
        topology.AddNode(Node("E", 100, 50, range: 60.0));
        topology.AddNode(Node("F", 150, 25, range: 60.0));

        IReadOnlyList<string> route = _router.FindRoute(topology, "A", "F");

        Assert.NotEmpty(route);
        Assert.Equal("A", route[0]);
        Assert.Equal("F", route[^1]);
        Assert.Equal(route.Count, route.Distinct().Count());
    }

    [Fact]
    public void FindRouteReturnsEmptyForDisconnectedComponents()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 60.0));
        topology.AddNode(Node("B", 50, 0, range: 60.0));
        topology.AddNode(Node("X", 1000, 0, range: 60.0));
        topology.AddNode(Node("Y", 1050, 0, range: 60.0));

        Assert.Empty(_router.FindRoute(topology, "A", "Y"));
    }

    [Fact]
    public void FindRouteIsDirectionalWhenTransmissionRangesDiffer()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0, range: 50.0));
        topology.AddNode(Node("B", 100, 0, range: 200.0));

        Assert.Empty(_router.FindRoute(topology, "A", "B"));
        Assert.Equal(["B", "A"], _router.FindRoute(topology, "B", "A"));
    }

    // ---------- Завади ----------

    [Fact]
    public void FindRouteReturnsEmptyWhenDirectLinkIsJammedAndNoRelayExists()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("C", 100, 0));
        topology.AddJammingZone(new JammingZone("jam", new Position(50, 0), 25.0, 1.0));

        Assert.Empty(_router.FindRoute(topology, "A", "C"));
    }

    [Theory]
    [InlineData(0.84, true)]
    [InlineData(0.85, false)]
    public void FindRouteUsesOnlyLinkUnlessIntensityReachesBlockThreshold(double intensity, bool routeExpected)
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 100, 0));
        topology.AddJammingZone(new JammingZone("jam", new Position(50, 0), 20.0, intensity));

        IReadOnlyList<string> route = _router.FindRoute(topology, "A", "B");

        if (routeExpected)
        {
            Assert.Equal(["A", "B"], route);
        }
        else
        {
            Assert.Empty(route);
        }
    }

    [Fact]
    public void FindRouteStillUsesPartiallyJammedLinkWhenItIsTheOnlyPath()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 100, 0));
        topology.AddJammingZone(new JammingZone("jam", new Position(50, 0), 20.0, 0.5));

        Assert.Equal(["A", "B"], _router.FindRoute(topology, "A", "B"));
    }

    [Theory]
    [InlineData(0.1, false)]  // пряма: 100 / 0.9^2 ≈ 123.5  < 141.4 (через B)
    [InlineData(0.2, true)]   // пряма: 100 / 0.8^2 = 156.25 > 141.4 (через B)
    public void FindRouteSwitchesToRelayWhenQualitySquaredPenaltyExceedsDetourCost(double intensity, bool expectRelay)
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 50, 50));
        topology.AddNode(Node("C", 100, 0));
        topology.AddJammingZone(new JammingZone("jam", new Position(50, 0), 10.0, intensity));

        IReadOnlyList<string> route = _router.FindRoute(topology, "A", "C");

        string[] expected = expectRelay ? ["A", "B", "C"] : ["A", "C"];
        Assert.Equal(expected, route);
    }

    [Fact]
    public void FindRouteReturnsDirectPathAfterJammingZoneIsDeactivated()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 50, 60));
        topology.AddNode(Node("C", 100, 0));
        var zone = new JammingZone("jam", new Position(50, 0), 25.0, 1.0);
        topology.AddJammingZone(zone);
        Assert.Equal(["A", "B", "C"], _router.FindRoute(topology, "A", "C"));

        zone.IsActive = false;

        Assert.Equal(["A", "C"], _router.FindRoute(topology, "A", "C"));
    }

    [Fact]
    public void FindRouteReturnsDirectPathAfterJammingZoneMovesAway()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 50, 60));
        topology.AddNode(Node("C", 100, 0));
        var zone = new JammingZone("jam", new Position(50, 0), 25.0, 1.0);
        topology.AddJammingZone(zone);
        Assert.Equal(["A", "B", "C"], _router.FindRoute(topology, "A", "C"));

        zone.MoveTo(new Position(500, 500));

        Assert.Equal(["A", "C"], _router.FindRoute(topology, "A", "C"));
    }

    // ---------- Заряд ретрансляторів ----------

    [Fact]
    public void FindRouteReturnsEmptyWhenOnlyRelayBatteryIsDepleted()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("B", 50, 60, battery: 0.0));
        topology.AddNode(Node("C", 100, 0));
        topology.AddJammingZone(new JammingZone("jam", new Position(50, 0), 25.0, 1.0));

        Assert.Empty(_router.FindRoute(topology, "A", "C"));
    }

    [Fact]
    public void FindRouteSkipsDepletedRelayAndUsesHealthyAlternative()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        topology.AddNode(Node("DEAD", 50, 60, battery: 0.0));
        topology.AddNode(Node("LIVE", 50, -60));
        topology.AddNode(Node("C", 100, 0));
        topology.AddJammingZone(new JammingZone("jam", new Position(50, 0), 25.0, 1.0));

        Assert.Equal(["A", "LIVE", "C"], _router.FindRoute(topology, "A", "C"));
    }

    [Fact]
    public void FindRouteReactsToRelayBatteryDrainingBetweenCalls()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("A", 0, 0));
        MeshNode relay = Node("B", 50, 60, battery: 5.0);
        topology.AddNode(relay);
        topology.AddNode(Node("C", 100, 0));
        topology.AddJammingZone(new JammingZone("jam", new Position(50, 0), 25.0, 1.0));
        Assert.Equal(["A", "B", "C"], _router.FindRoute(topology, "A", "C"));

        relay.ConsumeEnergy(5.0);

        Assert.Empty(_router.FindRoute(topology, "A", "C"));
    }
}