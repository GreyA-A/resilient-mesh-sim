using MeshSim.Desktop.Rendering;
using MeshSim.Engine;

namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Модель подання карти: зберігає топологію й розмір вікна та перебудовує сцену при їх зміні.
/// </summary>
public sealed class MapViewModel : ViewModelBase
{
    public const double ViewportPaddingPixels = 48.0;

    private NetworkTopology _topology = new();
    private MapScene _scene = MapScene.Empty;
    private double _viewportWidth;
    private double _viewportHeight;

    public NetworkTopology Topology => _topology;

    public MapScene Scene
    {
        get => _scene;
        private set => SetProperty(ref _scene, value);
    }

    public double ViewportWidth => _viewportWidth;

    public double ViewportHeight => _viewportHeight;

    /// <summary>
    /// Поточне перетворення світ → екран (null, поки вікно має нульовий розмір).
    /// </summary>
    public ViewportTransform? Transform { get; private set; }

    public void SetTopology(NetworkTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        _topology = topology;
        Rebuild();
    }

    public void Resize(double width, double height)
    {
        Guard.RequireNonNegativeFinite(width, nameof(width));
        Guard.RequireNonNegativeFinite(height, nameof(height));

        if (width == _viewportWidth && height == _viewportHeight)
        {
            return;
        }

        _viewportWidth = width;
        _viewportHeight = height;
        Rebuild();
    }

    /// <summary>
    /// Перебудовує сцену після зміни стану топології (заряд, завади, позиції).
    /// </summary>
    public void Refresh()
    {
        Rebuild();
    }

    private void Rebuild()
    {
        if (_viewportWidth <= 0.0 || _viewportHeight <= 0.0)
        {
            Transform = null;
            Scene = MapScene.Empty;
            return;
        }

        Transform = ViewportTransform.Fit(
            WorldBounds.FromTopology(_topology),
            _viewportWidth,
            _viewportHeight,
            ViewportPaddingPixels);
        Scene = MapSceneBuilder.Build(_topology, Transform);
    }
}