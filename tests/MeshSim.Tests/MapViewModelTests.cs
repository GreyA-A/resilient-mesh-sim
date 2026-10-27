using MeshSim.Core;
using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine;

namespace MeshSim.Tests;

public class MapViewModelTests
{
    private static NetworkTopology SampleTopology()
    {
        var topology = new NetworkTopology();
        topology.AddNode(new MeshNode("A", new Position(0, 0), 100.0));
        topology.AddNode(new MeshNode("B", new Position(80, 0), 100.0));
        return topology;
    }

    private static MapViewModel CreateViewModel()
    {
        var viewModel = new MapViewModel();
        viewModel.SetTopology(SampleTopology());
        return viewModel;
    }

    [Fact]
    public void NewViewModelHasEmptySceneAndNoTransform()
    {
        var viewModel = new MapViewModel();

        Assert.Same(MapScene.Empty, viewModel.Scene);
        Assert.Null(viewModel.Transform);
        Assert.Empty(viewModel.Topology.Nodes);
    }

    [Fact]
    public void SetTopologyKeepsSceneEmptyUntilViewportHasSize()
    {
        MapViewModel viewModel = CreateViewModel();

        Assert.Equal(2, viewModel.Topology.Nodes.Count);
        Assert.Same(MapScene.Empty, viewModel.Scene);
        Assert.Null(viewModel.Transform);
    }

    [Fact]
    public void ResizeBuildsSceneForCurrentTopology()
    {
        MapViewModel viewModel = CreateViewModel();

        viewModel.Resize(800.0, 600.0);

        Assert.Equal(2, viewModel.Scene.Nodes.Count);
        Assert.Single(viewModel.Scene.Links);
        Assert.NotNull(viewModel.Transform);
        Assert.Equal(800.0, viewModel.ViewportWidth);
        Assert.Equal(600.0, viewModel.ViewportHeight);
    }

    [Fact]
    public void ResizeWithSameSizeDoesNotRebuildScene()
    {
        MapViewModel viewModel = CreateViewModel();
        viewModel.Resize(800.0, 600.0);
        MapScene before = viewModel.Scene;

        viewModel.Resize(800.0, 600.0);

        Assert.Same(before, viewModel.Scene);
    }

    [Fact]
    public void ResizeWithNewSizeRebuildsScene()
    {
        MapViewModel viewModel = CreateViewModel();
        viewModel.Resize(800.0, 600.0);
        MapScene before = viewModel.Scene;

        viewModel.Resize(1000.0, 700.0);

        Assert.NotSame(before, viewModel.Scene);
    }

    [Theory]
    [InlineData(0.0, 600.0)]
    [InlineData(800.0, 0.0)]
    public void ResizeToZeroAreaClearsSceneAndTransform(double width, double height)
    {
        MapViewModel viewModel = CreateViewModel();
        viewModel.Resize(800.0, 600.0);

        viewModel.Resize(width, height);

        Assert.Same(MapScene.Empty, viewModel.Scene);
        Assert.Null(viewModel.Transform);
    }

    [Theory]
    [InlineData(double.NaN, 10.0)]
    [InlineData(10.0, double.NaN)]
    [InlineData(-1.0, 10.0)]
    [InlineData(10.0, -1.0)]
    [InlineData(double.PositiveInfinity, 10.0)]
    [InlineData(10.0, double.PositiveInfinity)]
    public void ResizeThrowsForInvalidSizes(double width, double height)
    {
        var viewModel = new MapViewModel();

        Assert.Throws<ArgumentOutOfRangeException>(() => viewModel.Resize(width, height));
    }

    [Fact]
    public void SceneChangeRaisesPropertyChanged()
    {
        MapViewModel viewModel = CreateViewModel();

        Assert.PropertyChanged(viewModel, nameof(MapViewModel.Scene), () => viewModel.Resize(800.0, 600.0));
    }

    [Fact]
    public void RefreshRebuildsSceneAfterTopologyStateChange()
    {
        MapViewModel viewModel = CreateViewModel();
        viewModel.Resize(800.0, 600.0);
        Assert.Single(viewModel.Scene.Links);

        viewModel.Topology.Nodes["B"].ConsumeEnergy(100.0);
        viewModel.Refresh();

        Assert.Empty(viewModel.Scene.Links);
        Assert.False(viewModel.Scene.Nodes[1].Style.IsActive);
    }

    [Fact]
    public void SetTopologyRebuildsSceneForExistingViewport()
    {
        var viewModel = new MapViewModel();
        viewModel.Resize(800.0, 600.0);

        viewModel.SetTopology(SampleTopology());

        Assert.Equal(2, viewModel.Scene.Nodes.Count);
    }

    [Fact]
    public void SetTopologyThrowsArgumentNullExceptionForNull()
    {
        var viewModel = new MapViewModel();

        Assert.Throws<ArgumentNullException>(() => viewModel.SetTopology(null!));
    }
}