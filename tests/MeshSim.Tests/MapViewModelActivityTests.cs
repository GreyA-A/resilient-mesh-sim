using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine;

namespace MeshSim.Tests;

public class MapViewModelActivityTests
{
    private static NetworkTopology SampleTopology()
    {
        var topology = new NetworkTopology();
        topology.AddNode(new MeshNode("A", new Position(0, 0), 100.0));
        topology.AddNode(new MeshNode("B", new Position(80, 0), 100.0));
        return topology;
    }

    private static PacketActivity ActivityAt(string node)
        => new("p1", node, "A", SimulationEventType.PacketForwarded, PacketDropReason.None);

    [Fact]
    public void NewViewModelHasNoActivity()
    {
        Assert.Null(new MapViewModel().Activity);
    }

    [Fact]
    public void SetActivityStoresActivityAndRebuildsSceneWithMarker()
    {
        var viewModel = new MapViewModel();
        viewModel.SetTopology(SampleTopology());
        viewModel.Resize(800.0, 600.0);

        viewModel.SetActivity(ActivityAt("B"));

        Assert.Equal(ActivityAt("B"), viewModel.Activity);
        Assert.True(viewModel.Scene.Marker.HasValue);
        Assert.Equal("p1", viewModel.Scene.Marker.GetValueOrDefault().PacketId);
        Assert.True(viewModel.Scene.Segment.HasValue);
    }

    [Fact]
    public void SetActivityNullRemovesMarker()
    {
        var viewModel = new MapViewModel();
        viewModel.SetTopology(SampleTopology());
        viewModel.Resize(800.0, 600.0);
        viewModel.SetActivity(ActivityAt("B"));

        viewModel.SetActivity(null);

        Assert.Null(viewModel.Activity);
        Assert.Null(viewModel.Scene.Marker);
    }

    [Fact]
    public void SetActivityBeforeViewportHasSizeKeepsSceneEmpty()
    {
        var viewModel = new MapViewModel();
        viewModel.SetTopology(SampleTopology());

        viewModel.SetActivity(ActivityAt("B"));

        Assert.Same(MapScene.Empty, viewModel.Scene);
        Assert.NotNull(viewModel.Activity);
    }

    [Fact]
    public void SetTopologyClearsActivity()
    {
        var viewModel = new MapViewModel();
        viewModel.SetTopology(SampleTopology());
        viewModel.SetActivity(ActivityAt("B"));

        viewModel.SetTopology(SampleTopology());

        Assert.Null(viewModel.Activity);
    }

    [Fact]
    public void ActivitySurvivesResize()
    {
        var viewModel = new MapViewModel();
        viewModel.SetTopology(SampleTopology());
        viewModel.Resize(800.0, 600.0);
        viewModel.SetActivity(ActivityAt("B"));

        viewModel.Resize(1000.0, 700.0);

        Assert.True(viewModel.Scene.Marker.HasValue);
    }
}