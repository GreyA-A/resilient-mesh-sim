using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using MeshSim.Core;
using MeshSim.Desktop.Editing;
using MeshSim.Desktop.Logging;
using MeshSim.Desktop.Rendering;
using MeshSim.Engine.IO;

namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Редактор сцени: додавання, видалення та перетягування вузлів і зон, перемикання зон, пакети.
/// Усі правки валідуються ScenarioValidator і застосовуються через SimulationViewModel.LoadScenario;
/// помилки потрапляють у ErrorMessage без винятків.
/// </summary>
public sealed class SceneEditorViewModel : ViewModelBase
{
    /// <summary>
    /// Кількість знаків після коми для координат, отриманих перетягуванням.
    /// </summary>
    public const int DecimalPlaces = 1;

    private readonly SimulationViewModel _simulation;
    private readonly IAppLogger _logger;

    private string? _nodeId = string.Empty;
    private string? _nodeX = "0";
    private string? _nodeY = "0";
    private string? _nodeRange = "140";
    private string? _nodeBattery = "100";
    private string? _zoneId = string.Empty;
    private string? _zoneX = "0";
    private string? _zoneY = "0";
    private string? _zoneRadius = "30";
    private string? _zoneIntensity = "0.5";
    private string? _packetId = string.Empty;
    private string? _packetSource = string.Empty;
    private string? _packetDestination = string.Empty;
    private string? _packetTtl = "8";
    private string? _packetPayloadBytes = "64";
    private string? _packetScheduledTimeMs = "0";
    private string? _errorMessage;
    private PacketRow? _selectedPacket;
    private IReadOnlyList<PacketDefinition>? _listedPackets;

    private MapTarget? _dragTarget;
    private ScreenPoint _pressPoint;
    private Position _dragOrigin;
    private bool _dragMoved;

    public SceneEditorViewModel(SimulationViewModel simulation, IAppLogger logger)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(logger);

        _simulation = simulation;
        _logger = logger;

        AddNodeCommand = new RelayCommand(AddNode, CanEdit);
        RemoveNodeCommand = new RelayCommand(RemoveNode, CanRemoveNode);
        AddZoneCommand = new RelayCommand(AddZone, CanEdit);
        RemoveZoneCommand = new RelayCommand(RemoveZone, CanRemoveZone);
        ToggleZoneCommand = new RelayCommand(ToggleZone, CanRemoveZone);
        AddPacketCommand = new RelayCommand(AddPacket, CanEdit);
        RemovePacketCommand = new RelayCommand(RemovePacket, CanRemovePacket);

        RefreshPacketsIfChanged();

        simulation.PropertyChanged += OnSimulationPropertyChanged;
        simulation.Map.PropertyChanged += OnMapPropertyChanged;
    }

    public SimulationViewModel Simulation => _simulation;
    public MapViewModel Map => _simulation.Map;
    public RelayCommand AddNodeCommand { get; }
    public RelayCommand RemoveNodeCommand { get; }
    public RelayCommand AddZoneCommand { get; }
    public RelayCommand RemoveZoneCommand { get; }
    public RelayCommand ToggleZoneCommand { get; }
    public RelayCommand AddPacketCommand { get; }
    public RelayCommand RemovePacketCommand { get; }

    /// <summary>
    /// Редагування дозволене, поки симуляція не виконується.
    /// </summary>
    public bool IsEditingEnabled => _simulation.State != SimulationState.Running;

    public bool IsDragging => _dragTarget is not null;

    public string SelectionText
    {
        get
        {
            if (Map.Selection is not { } target)
            {
                return "Нічого не вибрано.";
            }

            return string.Concat("Вибрано ", DescribeTarget(target));
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => _errorMessage is not null;

    // ---------- Поля форми: вузол ----------

    public string? NodeId
    {
        get => _nodeId;
        set => SetProperty(ref _nodeId, value);
    }

    public string? NodeX
    {
        get => _nodeX;
        set => SetProperty(ref _nodeX, value);
    }

    public string? NodeY
    {
        get => _nodeY;
        set => SetProperty(ref _nodeY, value);
    }

    public string? NodeRange
    {
        get => _nodeRange;
        set => SetProperty(ref _nodeRange, value);
    }

    public string? NodeBattery
    {
        get => _nodeBattery;
        set => SetProperty(ref _nodeBattery, value);
    }

    // ---------- Поля форми: зона ----------

    public string? ZoneId
    {
        get => _zoneId;
        set => SetProperty(ref _zoneId, value);
    }

    public string? ZoneX
    {
        get => _zoneX;
        set => SetProperty(ref _zoneX, value);
    }

    public string? ZoneY
    {
        get => _zoneY;
        set => SetProperty(ref _zoneY, value);
    }

    public string? ZoneRadius
    {
        get => _zoneRadius;
        set => SetProperty(ref _zoneRadius, value);
    }

    public string? ZoneIntensity
    {
        get => _zoneIntensity;
        set => SetProperty(ref _zoneIntensity, value);
    }

    // ---------- Поля форми: пакет ----------

    public string? PacketId
    {
        get => _packetId;
        set => SetProperty(ref _packetId, value);
    }

    public string? PacketSource
    {
        get => _packetSource;
        set => SetProperty(ref _packetSource, value);
    }

    public string? PacketDestination
    {
        get => _packetDestination;
        set => SetProperty(ref _packetDestination, value);
    }

    public string? PacketTtl
    {
        get => _packetTtl;
        set => SetProperty(ref _packetTtl, value);
    }

    public string? PacketPayloadBytes
    {
        get => _packetPayloadBytes;
        set => SetProperty(ref _packetPayloadBytes, value);
    }

    public string? PacketScheduledTimeMs
    {
        get => _packetScheduledTimeMs;
        set => SetProperty(ref _packetScheduledTimeMs, value);
    }

    // ---------- Список пакетів ----------

    public ObservableCollection<PacketRow> Packets { get; } = new();

    public PacketRow? SelectedPacket
    {
        get => _selectedPacket;
        set
        {
            if (SetProperty(ref _selectedPacket, value))
            {
                RemovePacketCommand.NotifyCanExecuteChanged();
            }
        }
    }

    // ---------- Події миші (їх лише делегує MapControl) ----------

    /// <summary>
    /// Вибирає об'єкт під курсором і, якщо редагування дозволене, починає перетягування.
    /// </summary>
    public void PointerPressed(ScreenPoint point)
    {
        MapTarget? hit = MapHitTester.Hit(Map.Scene, point);
        Map.SetSelection(hit);

        if (hit is not { } target || !IsEditingEnabled)
        {
            return;
        }

        _dragTarget = target;
        _dragMoved = false;
        _pressPoint = point;
        _dragOrigin = OriginOf(target);
        Map.BeginDrag();
    }

    /// <summary>
    /// Переміщує перетягуваний об'єкт: нова позиція = початкова + зсув курсора у світових координатах
    /// (з округленням), тож об'єкт не стрибає центром до курсора.
    /// </summary>
    public void PointerMoved(ScreenPoint point)
    {
        if (_dragTarget is not { } target || Map.Transform is not { } transform)
        {
            return;
        }

        Position pressWorld = transform.ToWorld(_pressPoint);
        Position pointerWorld = transform.ToWorld(point);
        double x = Math.Round(_dragOrigin.X + (pointerWorld.X - pressWorld.X), DecimalPlaces);
        double y = Math.Round(_dragOrigin.Y + (pointerWorld.Y - pressWorld.Y), DecimalPlaces);

        ScenarioDefinition candidate = target.Kind == MapTargetKind.Node
            ? ScenarioEditing.MoveNode(_simulation.Scenario, target.Id, x, y)
            : ScenarioEditing.MoveZone(_simulation.Scenario, target.Id, x, y);

        if (Apply(candidate))
        {
            _dragMoved = true;
        }
    }

    /// <summary>
    /// Завершує перетягування (відпускання кнопки або втрата захоплення миші).
    /// </summary>
    public void PointerReleased()
    {
        if (_dragTarget is not { } target)
        {
            return;
        }

        Map.EndDrag();
        _dragTarget = null;

        if (_dragMoved)
        {
            _logger.LogInfo(string.Concat("Переміщено ", DescribeTarget(target), "."));
            _dragMoved = false;
        }
    }

    // ---------- Доступність команд ----------

    private bool CanEdit()
    {
        return IsEditingEnabled;
    }

    private bool CanRemoveNode()
    {
        return IsEditingEnabled && Map.Selection is { Kind: MapTargetKind.Node };
    }

    private bool CanRemoveZone()
    {
        return IsEditingEnabled && Map.Selection is { Kind: MapTargetKind.Zone };
    }

    private bool CanRemovePacket()
    {
        return IsEditingEnabled && SelectedPacket is not null;
    }

    // ---------- Команди: вузли ----------

    private void AddNode()
    {
        if (!TryReadDouble(NodeX, "X вузла", out double x)
            || !TryReadDouble(NodeY, "Y вузла", out double y)
            || !TryReadDouble(NodeRange, "Радіус передачі", out double range)
            || !TryReadDouble(NodeBattery, "Заряд батареї", out double battery))
        {
            return;
        }

        var node = new NodeDefinition
        {
            Id = Normalize(NodeId),
            X = x,
            Y = y,
            TransmissionRangeMeters = range,
            BatteryLevelPercent = battery
        };

        if (Apply(ScenarioEditing.AddNode(_simulation.Scenario, node)))
        {
            _logger.LogInfo(string.Concat("Додано вузол ", node.Id, "."));
        }
    }

    private void RemoveNode()
    {
        if (Map.Selection is not { Kind: MapTargetKind.Node } target)
        {
            SetError("Спершу виберіть вузол на карті.");
            return;
        }

        if (Apply(ScenarioEditing.RemoveNode(_simulation.Scenario, target.Id)))
        {
            Map.SetSelection(null);
            _logger.LogInfo(string.Concat("Видалено вузол ", target.Id, "."));
        }
    }

    // ---------- Команди: зони ----------

    private void AddZone()
    {
        if (!TryReadDouble(ZoneX, "X зони", out double x)
            || !TryReadDouble(ZoneY, "Y зони", out double y)
            || !TryReadDouble(ZoneRadius, "Радіус зони", out double radius)
            || !TryReadDouble(ZoneIntensity, "Інтенсивність завад", out double intensity))
        {
            return;
        }

        var zone = new JammingZoneDefinition
        {
            Id = Normalize(ZoneId),
            X = x,
            Y = y,
            RadiusMeters = radius,
            JammingIntensity = intensity,
            IsActive = true
        };

        if (Apply(ScenarioEditing.AddZone(_simulation.Scenario, zone)))
        {
            _logger.LogInfo(string.Concat("Додано зону завад ", zone.Id, "."));
        }
    }

    private void RemoveZone()
    {
        if (Map.Selection is not { Kind: MapTargetKind.Zone } target)
        {
            SetError("Спершу виберіть зону завад на карті.");
            return;
        }

        if (Apply(ScenarioEditing.RemoveZone(_simulation.Scenario, target.Id)))
        {
            Map.SetSelection(null);
            _logger.LogInfo(string.Concat("Видалено зону завад ", target.Id, "."));
        }
    }

    private void ToggleZone()
    {
        if (Map.Selection is not { Kind: MapTargetKind.Zone } target)
        {
            SetError("Спершу виберіть зону завад на карті.");
            return;
        }

        if (Apply(ScenarioEditing.ToggleZone(_simulation.Scenario, target.Id)))
        {
            _logger.LogInfo(string.Concat("Перемкнуто активність зони завад ", target.Id, "."));
        }
    }

    // ---------- Команди: пакети ----------

    private void AddPacket()
    {
        if (!TryReadInt(PacketTtl, "TTL", out int ttl)
            || !TryReadInt(PacketPayloadBytes, "Розмір навантаження", out int payloadBytes)
            || !TryReadDouble(PacketScheduledTimeMs, "Час відправлення", out double scheduledTimeMs))
        {
            return;
        }

        var packet = new PacketDefinition
        {
            Id = Normalize(PacketId),
            SourceNodeId = Normalize(PacketSource),
            DestinationNodeId = Normalize(PacketDestination),
            Ttl = ttl,
            PayloadBytes = payloadBytes,
            ScheduledTimeMs = scheduledTimeMs
        };

        if (Apply(ScenarioEditing.AddPacket(_simulation.Scenario, packet)))
        {
            _logger.LogInfo(string.Concat("Додано пакет ", packet.Id, "."));
        }
    }

    private void RemovePacket()
    {
        if (SelectedPacket is null)
        {
            SetError("Спершу виберіть пакет у списку.");
            return;
        }

        string packetId = SelectedPacket.Id;

        if (Apply(ScenarioEditing.RemovePacket(_simulation.Scenario, packetId)))
        {
            _logger.LogInfo(string.Concat("Видалено пакет ", packetId, "."));
        }
    }

    // ---------- Застосування змін та помилки ----------

    /// <summary>
    /// Єдина точка застосування правки: перевіряє, що редагування дозволене, валідує кандидата
    /// і лише тоді замінює сценарій. RelayCommand.Execute не перевіряє CanExecute, тож захист тут.
    /// </summary>
    private bool Apply(ScenarioDefinition candidate)
    {
        if (!IsEditingEnabled)
        {
            SetError("Редагування недоступне під час виконання симуляції. Призупиніть її.");
            return false;
        }

        IReadOnlyList<string> errors = ScenarioValidator.Validate(candidate);

        if (errors.Count > 0)
        {
            SetError(string.Concat("Зміну не застосовано: ", string.Join(" ", errors)));
            return false;
        }

        _simulation.LoadScenario(candidate);
        ClearError();
        return true;
    }

    private bool TryReadDouble(string? text, string label, out double value)
    {
        if (double.TryParse(Normalize(text), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        SetError(string.Concat("Поле «", label, "» має бути числом (десяткова крапка, наприклад 12.5)."));
        return false;
    }

    private bool TryReadInt(string? text, string label, out int value)
    {
        if (int.TryParse(Normalize(text), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        SetError(string.Concat("Поле «", label, "» має бути цілим числом."));
        return false;
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
    }

    private void ClearError()
    {
        ErrorMessage = null;
    }

    private static string Normalize(string? text)
    {
        return (text ?? string.Empty).Trim();
    }

    private static string DescribeTarget(MapTarget target)
    {
        return target.Kind == MapTargetKind.Node
            ? string.Concat("вузол ", target.Id)
            : string.Concat("зону завад ", target.Id);
    }

    private Position OriginOf(MapTarget target)
    {
        if (target.Kind == MapTargetKind.Node)
        {
            return Map.Topology.Nodes[target.Id].Position;
        }

        return Map.Topology.JammingZones
            .First(zone => string.Equals(zone.Id, target.Id, StringComparison.Ordinal))
            .Center;
    }

    // ---------- Синхронізація зі станом симуляції, картою та сценарієм ----------

    private void OnSimulationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SimulationViewModel.State))
        {
            OnPropertyChanged(nameof(IsEditingEnabled));
            RefreshCommands();
        }
        else if (e.PropertyName == nameof(SimulationViewModel.Scenario))
        {
            RefreshPacketsIfChanged();
        }
    }

    private void OnMapPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapViewModel.Selection))
        {
            OnPropertyChanged(nameof(SelectionText));
            RefreshCommands();
        }
    }

    /// <summary>
    /// Оновлює список пакетів, лише якщо змінився сам список (операції над вузлами й зонами
    /// передають те саме посилання, тож під час перетягування список не перебудовується).
    /// </summary>
    private void RefreshPacketsIfChanged()
    {
        IReadOnlyList<PacketDefinition> current = _simulation.Scenario.Packets;

        if (ReferenceEquals(current, _listedPackets))
        {
            return;
        }

        _listedPackets = current;
        Packets.Clear();

        foreach (PacketDefinition packet in current)
        {
            Packets.Add(ToRow(packet));
        }

        SelectedPacket = null;
    }

    private static PacketRow ToRow(PacketDefinition packet)
    {
        string description = string.Create(
            CultureInfo.InvariantCulture,
            $"{packet.Id}: {packet.SourceNodeId} → {packet.DestinationNodeId}, TTL {packet.Ttl}, t = {packet.ScheduledTimeMs} мс");

        return new PacketRow(packet.Id, description);
    }

    private void RefreshCommands()
    {
        AddNodeCommand.NotifyCanExecuteChanged();
        RemoveNodeCommand.NotifyCanExecuteChanged();
        AddZoneCommand.NotifyCanExecuteChanged();
        RemoveZoneCommand.NotifyCanExecuteChanged();
        ToggleZoneCommand.NotifyCanExecuteChanged();
        AddPacketCommand.NotifyCanExecuteChanged();
        RemovePacketCommand.NotifyCanExecuteChanged();
    }
}