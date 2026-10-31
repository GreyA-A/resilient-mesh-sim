using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine;

namespace MeshSim.Tests;

public class MapViewModelEditingTests
{
    private static NetworkTopology Topology()
    {
        var topology = new NetworkTopology();
        topology.AddNode(new MeshNode("A", new Position(0, 0), 100.0));
        topology.AddNode(new MeshNode("B", new Position(80, 0), 100.0));
        topology.AddJammingZone(new JammingZone("Z", new Position(40, 40), 10.0, 0.5));
        return topology;
    }

    private static MapViewModel Create(double width = 800.0, double height = 600.0)
    {
        var viewModel = new MapViewModel();
        viewModel.SetTopology(Topology());
        viewModel.Resize(width, height);
        return viewModel;
    }

    private static readonly MapTarget NodeA = new(MapTargetKind.Node, "A");
    private static readonly MapTarget ZoneZ = new(MapTargetKind.Zone, "Z");

    // ---------- Виділення ----------

    [Fact]
    public void NewViewModelHasNoSelection()
    {
        Assert.Null(new MapViewModel().Selection);
    }

    [Fact]
    public void SetSelectionStoresTargetAndAddsRingToScene()
    {
        MapViewModel viewModel = Create();

        viewModel.SetSelection(NodeA);

        Assert.Equal(NodeA, viewModel.Selection);
        Assert.True(viewModel.Scene.Selection.HasValue);
    }

    [Fact]
    public void SetSelectionNullRemovesSelectionAndRing()
    {
        MapViewModel viewModel = Create();
        viewModel.SetSelection(NodeA);

        viewModel.SetSelection(null);

        Assert.Null(viewModel.Selection);
        Assert.Null(viewModel.Scene.Selection);
    }

    [Fact]
    public void SetSelectionRaisesPropertyChanged()
    {
        MapViewModel viewModel = Create();

        Assert.PropertyChanged(viewModel, nameof(MapViewModel.Selection), () => viewModel.SetSelection(NodeA));
    }

    [Theory]
    [InlineData(MapTargetKind.Node, "A")]
    [InlineData(MapTargetKind.Zone, "Z")]
    public void SetTopologyKeepsSelectionWhenTargetStillExists(MapTargetKind kind, string id)
    {
        MapViewModel viewModel = Create();
        viewModel.SetSelection(new MapTarget(kind, id));

        viewModel.SetTopology(Topology());

        Assert.Equal(new MapTarget(kind, id), viewModel.Selection);
    }

    [Theory]
    [InlineData(MapTargetKind.Node, "A")]
    [InlineData(MapTargetKind.Zone, "Z")]
    public void SetTopologyClearsSelectionWhenTargetNoLongerExists(MapTargetKind kind, string id)
    {
        MapViewModel viewModel = Create();
        viewModel.SetSelection(new MapTarget(kind, id));

        viewModel.SetTopology(new NetworkTopology());

        Assert.Null(viewModel.Selection);
    }

    // ---------- Перетягування ----------

    [Fact]
    public void BeginDragFreezesTransformUntilEndDrag()
    {
        MapViewModel viewModel = Create();
        ViewportTransform? before = viewModel.Transform;
        Assert.False(viewModel.IsDragging);

        viewModel.BeginDrag();
        var farAway = new NetworkTopology();
        farAway.AddNode(new MeshNode("FAR", new Position(5000, 5000), 100.0));
        viewModel.SetTopology(farAway);

        Assert.True(viewModel.IsDragging);
        Assert.Same(before, viewModel.Transform);

        viewModel.EndDrag();

        Assert.False(viewModel.IsDragging);
        Assert.NotSame(before, viewModel.Transform);
    }

    [Fact]
    public void BeginDragWithoutViewportDoesNotFreezeAnything()
    {
        var viewModel = new MapViewModel();
        viewModel.SetTopology(Topology());

        viewModel.BeginDrag();

        Assert.False(viewModel.IsDragging);
        Assert.Null(viewModel.Transform);
    }

    [Fact]
    public void ZeroSizedViewportClearsTransformEvenWhileDragging()
    {
        MapViewModel viewModel = Create();
        viewModel.BeginDrag();

        viewModel.Resize(0.0, 0.0);

        Assert.Null(viewModel.Transform);
        Assert.Same(MapScene.Empty, viewModel.Scene);

        viewModel.EndDrag();

        Assert.False(viewModel.IsDragging);
    }
}