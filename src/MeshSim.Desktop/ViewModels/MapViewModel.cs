using MeshSim.Desktop.Rendering;
using MeshSim.Engine;

namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Модель подання карти: зберігає топологію, розмір вікна, активність пакета та виділення
/// і перебудовує сцену при їх зміні. Під час перетягування вид «заморожується».
/// </summary>
public sealed class MapViewModel : ViewModelBase
{
    public const double ViewportPaddingPixels = 48.0;

    private NetworkTopology _topology = new();
    private MapScene _scene = MapScene.Empty;
    private PacketActivity? _activity;
    private MapTarget? _selection;
    private ViewportTransform? _frozenTransform;
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
    /// Остання подія пакета, що відображається на карті (null, якщо нічого не відбувалось).
    /// </summary>
    public PacketActivity? Activity => _activity;

    /// <summary>
    /// Вибраний вузол або зона завад (null, якщо нічого не вибрано).
    /// </summary>
    public MapTarget? Selection
    {
        get => _selection;
        private set => SetProperty(ref _selection, value);
    }

    /// <summary>
    /// Чи триває перетягування (вид заморожено, тож масштаб і зсув не змінюються під курсором).
    /// </summary>
    public bool IsDragging => _frozenTransform is not null;

    /// <summary>
    /// Поточне перетворення світ → екран (null, поки вікно має нульовий розмір).
    /// </summary>
    public ViewportTransform? Transform { get; private set; }

    /// <summary>
    /// Замінює топологію; активність пакета скидається, а виділення зберігається, лише якщо
    /// вибраний об'єкт існує в новій топології.
    /// </summary>
    public void SetTopology(NetworkTopology topology)
    {
        ArgumentNullException.ThrowIfNull(topology);

        _topology = topology;
        _activity = null;

        if (_selection is { } current && !Exists(topology, current))
        {
            Selection = null;
        }

        Rebuild();
    }

    /// <summary>
    /// Задає (або прибирає, якщо null) активність пакета й перебудовує сцену з актуальним станом вузлів.
    /// </summary>
    public void SetActivity(PacketActivity? activity)
    {
        _activity = activity;
        Rebuild();
    }

    /// <summary>
    /// Задає (або прибирає, якщо null) виділення й перебудовує сцену.
    /// </summary>
    public void SetSelection(MapTarget? selection)
    {
        Selection = selection;
        Rebuild();
    }

    /// <summary>
    /// Заморожує поточне перетворення світ → екран на час перетягування.
    /// Якщо вікно має нульовий розмір, нічого не заморожується.
    /// </summary>
    public void BeginDrag()
    {
        _frozenTransform = Transform;
    }

    /// <summary>
    /// Розморожує вид і вписує сцену у вікно заново.
    /// </summary>
    public void EndDrag()
    {
        _frozenTransform = null;
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

    private static bool Exists(NetworkTopology topology, MapTarget target)
    {
        return target.Kind == MapTargetKind.Node
            ? topology.Nodes.ContainsKey(target.Id)
            : topology.JammingZones.Any(zone => string.Equals(zone.Id, target.Id, StringComparison.Ordinal));
    }

    private void Rebuild()
    {
        if (_viewportWidth <= 0.0 || _viewportHeight <= 0.0)
        {
            Transform = null;
            Scene = MapScene.Empty;
            return;
        }

        Transform = _frozenTransform ?? ViewportTransform.Fit(
            WorldBounds.FromTopology(_topology),
            _viewportWidth,
            _viewportHeight,
            ViewportPaddingPixels);
        Scene = MapSceneBuilder.Build(_topology, Transform, _activity, _selection);
    }
}