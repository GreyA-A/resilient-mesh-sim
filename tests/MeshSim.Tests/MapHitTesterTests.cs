using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Engine;

namespace MeshSim.Tests;

public class MapHitTesterTests
{
    private static MeshNode Node(string id, double x, double y)
        => new(id, new Position(x, y), 100.0);

    /// <summary>
    /// Перетворення: екран = (2x, 200 - 2y).
    /// </summary>
    private static ViewportTransform Transform()
        => ViewportTransform.Fit(new WorldBounds(0.0, 0.0, 100.0, 100.0), 200.0, 200.0, 0.0);

    private static MapScene Scene(NetworkTopology topology) => MapSceneBuilder.Build(topology, Transform());

    private static NetworkTopology OverlappingNodes()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("N1", 50, 50)); // екран (100, 100)
        topology.AddNode(Node("N2", 54, 50)); // екран (108, 100)
        return topology;
    }

    // ---------- MapTarget ----------

    [Fact]
    public void MapTargetStoresKindAndId()
    {
        var target = new MapTarget(MapTargetKind.Zone, "z1");

        Assert.Equal(MapTargetKind.Zone, target.Kind);
        Assert.Equal("z1", target.Id);
    }

    // ---------- Вузли ----------

    [Fact]
    public void HitReturnsNodeWhenPointIsInsideItsRadius()
    {
        MapScene scene = Scene(OverlappingNodes());

        MapTarget? hit = MapHitTester.Hit(scene, new ScreenPoint(100, 103));

        Assert.True(hit.HasValue);
        Assert.Equal(new MapTarget(MapTargetKind.Node, "N1"), hit.GetValueOrDefault());
    }

    [Fact]
    public void HitCountsPointExactlyOnNodeBoundary()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("N1", 50, 50));

        MapTarget? hit = MapHitTester.Hit(Scene(topology), new ScreenPoint(109, 100));

        Assert.Equal("N1", hit.GetValueOrDefault().Id);
    }

    [Fact]
    public void HitReturnsNullJustOutsideNodeRadius()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("N1", 50, 50));

        Assert.Null(MapHitTester.Hit(Scene(topology), new ScreenPoint(109.5, 100)));
    }

    [Theory]
    [InlineData(106.0, "N2")] // другий вузол ближчий: замінює першого
    [InlineData(102.0, "N1")] // другий усередині радіуса, але далі: перший лишається
    public void HitChoosesNearestOfOverlappingNodes(double pointX, string expectedId)
    {
        MapScene scene = Scene(OverlappingNodes());

        MapTarget? hit = MapHitTester.Hit(scene, new ScreenPoint(pointX, 100));

        Assert.Equal(expectedId, hit.GetValueOrDefault().Id);
        Assert.Equal(MapTargetKind.Node, hit.GetValueOrDefault().Kind);
    }

    // ---------- Зони ----------

    [Fact]
    public void HitReturnsZoneWhenPointIsInsideItsCircleAndNoNodeIsUnderneath()
    {
        var topology = new NetworkTopology();
        topology.AddJammingZone(new JammingZone("z", new Position(50, 50), 20.0, 0.5)); // екран (100,100), радіус 40

        MapTarget? hit = MapHitTester.Hit(Scene(topology), new ScreenPoint(120, 100));

        Assert.Equal(new MapTarget(MapTargetKind.Zone, "z"), hit.GetValueOrDefault());
    }

    [Fact]
    public void HitPrefersNodeOverZoneAtSamePoint()
    {
        var topology = new NetworkTopology();
        topology.AddNode(Node("N1", 50, 50));
        topology.AddJammingZone(new JammingZone("z", new Position(50, 50), 20.0, 0.5));

        MapTarget? hit = MapHitTester.Hit(Scene(topology), new ScreenPoint(100, 100));

        Assert.Equal(MapTargetKind.Node, hit.GetValueOrDefault().Kind);
        Assert.Equal("N1", hit.GetValueOrDefault().Id);
    }

    [Theory]
    [InlineData(true)]  // менша зона додана після більшої
    [InlineData(false)] // менша зона додана перед більшою
    public void HitChoosesSmallestOfNestedZones(bool bigFirst)
    {
        var big = new JammingZone("big", new Position(50, 50), 40.0, 0.5);
        var small = new JammingZone("small", new Position(50, 50), 10.0, 0.5);
        var topology = new NetworkTopology();
        topology.AddJammingZone(bigFirst ? big : small);
        topology.AddJammingZone(bigFirst ? small : big);

        MapTarget? hit = MapHitTester.Hit(Scene(topology), new ScreenPoint(110, 100));

        Assert.Equal("small", hit.GetValueOrDefault().Id);
    }

    [Fact]
    public void HitReturnsNullWhenPointIsOutsideZone()
    {
        var topology = new NetworkTopology();
        topology.AddJammingZone(new JammingZone("z", new Position(50, 50), 10.0, 0.5));

        Assert.Null(MapHitTester.Hit(Scene(topology), new ScreenPoint(10, 10)));
    }

    // ---------- Граничні випадки ----------

    [Fact]
    public void HitReturnsNullForEmptyScene()
    {
        Assert.Null(MapHitTester.Hit(MapScene.Empty, new ScreenPoint(0, 0)));
    }

    [Fact]
    public void HitThrowsArgumentNullExceptionForNullScene()
    {
        Assert.Throws<ArgumentNullException>(() => MapHitTester.Hit(null!, new ScreenPoint(0, 0)));
    }
}