using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using MeshSim.Desktop.Logging;
using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.Services;
using MeshSim.Engine;
using MeshSim.Engine.IO;

namespace MeshSim.Desktop.ViewModels;

/// <summary>
/// Керування симуляцією: Play/Pause/Step/Reset, швидкість, журнал подій, метрики, активність пакета на карті
/// та заміна сценарію (редагування сцени).
/// </summary>
public sealed class SimulationViewModel : ViewModelBase
{
    /// <summary>
    /// Інтервал тіку при множнику швидкості 1×.
    /// </summary>
    public static readonly TimeSpan BaseTickInterval = TimeSpan.FromMilliseconds(800);

    private const int DefaultSpeedIndex = 2;

    private readonly ISimulationTimer _timer;
    private readonly IAppLogger _logger;
    private readonly Dictionary<string, string> _lastNodeByPacket = new(StringComparer.Ordinal);

    private ScenarioDefinition _scenario;
    private SimulationEngine _engine;
    private SimulationState _state = SimulationState.Ready;
    private SimulationSpeedOption _selectedSpeed;

    public SimulationViewModel(MapViewModel map, ScenarioDefinition scenario, ISimulationTimer timer, IAppLogger logger)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(timer);
        ArgumentNullException.ThrowIfNull(logger);

        Map = map;
        _scenario = scenario;
        _timer = timer;
        _logger = logger;

        Metrics = new MetricsViewModel();
        SpeedOptions =
        [
            new SimulationSpeedOption("0.25×", 0.25),
            new SimulationSpeedOption("0.5×", 0.5),
            new SimulationSpeedOption("1×", 1.0),
            new SimulationSpeedOption("2×", 2.0),
            new SimulationSpeedOption("4×", 4.0)
        ];
        _selectedSpeed = SpeedOptions[DefaultSpeedIndex];
        _timer.Interval = _selectedSpeed.GetInterval(BaseTickInterval);
        _timer.Tick += OnTick;

        PlayCommand = new RelayCommand(Play, CanPlay);
        PauseCommand = new RelayCommand(Pause, CanPause);
        StepCommand = new RelayCommand(Step, CanStep);
        ResetCommand = new RelayCommand(Reset);

        ScenarioRuntime runtime = ScenarioFactory.Build(scenario);
        _engine = runtime.Simulation;
        ApplyRuntime(runtime);
    }

    public MapViewModel Map { get; }

    public MetricsViewModel Metrics { get; }

    /// <summary>
    /// Поточний сценарій (джерело правди для карти, редактора та майбутнього збереження у файл).
    /// </summary>
    public ScenarioDefinition Scenario => _scenario;

    /// <summary>
    /// Журнал подій; найновіші рядки зверху.
    /// </summary>
    public ObservableCollection<string> EventLog { get; } = new();

    public IReadOnlyList<SimulationSpeedOption> SpeedOptions { get; }

    public RelayCommand PlayCommand { get; }

    public RelayCommand PauseCommand { get; }

    public RelayCommand StepCommand { get; }

    public RelayCommand ResetCommand { get; }

    public SimulationSpeedOption SelectedSpeed
    {
        get => _selectedSpeed;
        set
        {
            if (value is null)
            {
                return;
            }

            if (SetProperty(ref _selectedSpeed, value))
            {
                _timer.Interval = value.GetInterval(BaseTickInterval);
            }
        }
    }

    public SimulationState State
    {
        get => _state;
        private set
        {
            if (SetProperty(ref _state, value))
            {
                OnPropertyChanged(nameof(StateText));
            }
        }
    }

    public string StateText => DescribeState(_state);

    public static string DescribeState(SimulationState state)
    {
        return state switch
        {
            SimulationState.Ready => "Готово",
            SimulationState.Running => "Виконується",
            SimulationState.Paused => "Пауза",
            SimulationState.Finished => "Завершено",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Невідомий стан симуляції.")
        };
    }

    /// <summary>
    /// Замінює сценарій: зупиняє таймер, будує свіжу топологію й рушій і переводить стан у «Готово».
    /// Некоректний сценарій відхиляється InvalidDataException до будь-якої зміни стану.
    /// </summary>
    public void LoadScenario(ScenarioDefinition scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        ReplaceRuntime(scenario);
    }

    private bool CanPlay()
    {
        return _state != SimulationState.Running && _engine.HasPendingEvents;
    }

    private bool CanPause()
    {
        return _state == SimulationState.Running;
    }

    private bool CanStep()
    {
        return _state is not (SimulationState.Running or SimulationState.Finished) && _engine.HasPendingEvents;
    }

    private void Play()
    {
        if (_state == SimulationState.Running)
        {
            return;
        }

        _timer.Start();
        State = SimulationState.Running;
        _logger.LogInfo("Симуляцію запущено.");
        NotifyCommands();
    }

    private void Pause()
    {
        if (_state != SimulationState.Running)
        {
            return;
        }

        _timer.Pause();
        State = SimulationState.Paused;
        _logger.LogInfo("Симуляцію призупинено.");
        NotifyCommands();
    }

    private void Step()
    {
        if (_state is SimulationState.Running or SimulationState.Finished)
        {
            return;
        }

        if (ExecuteStepAndCheckFinished())
        {
            FinishRun();
            return;
        }

        State = SimulationState.Paused;
        NotifyCommands();
    }

    private void Reset()
    {
        ReplaceRuntime(_scenario);
        _logger.LogInfo("Симуляцію скинуто до початкового стану сценарію.");
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_state != SimulationState.Running)
        {
            return;
        }

        if (ExecuteStepAndCheckFinished())
        {
            FinishRun();
        }
    }

    /// <summary>
    /// Виконує один крок і повідомляє, чи прогін завершено (подій більше немає).
    /// </summary>
    private bool ExecuteStepAndCheckFinished()
    {
        return !TryExecuteStep() || !_engine.HasPendingEvents;
    }

    private bool TryExecuteStep()
    {
        SimulationEvent? stepped = _engine.Step();
        if (stepped is not { } logged)
        {
            return false;
        }

        EventLog.Insert(0, EventTextFormatter.FormatEvent(logged));

        _lastNodeByPacket.TryGetValue(logged.Packet.Id, out string? previousNodeId);
        _lastNodeByPacket[logged.Packet.Id] = logged.CurrentNodeId;

        Metrics.Update(_engine.GetSnapshot(), _engine.CurrentTimeMs);
        Map.SetActivity(new PacketActivity(
            logged.Packet.Id,
            logged.CurrentNodeId,
            previousNodeId,
            logged.EventType,
            logged.DropReason));

        return true;
    }

    private void FinishRun()
    {
        _timer.Pause();
        State = SimulationState.Finished;
        _logger.LogInfo(string.Create(
            CultureInfo.InvariantCulture,
            $"Симуляцію завершено: доставлено {Metrics.TotalPacketsDelivered} з {Metrics.TotalPacketsSent}, PDR {Metrics.PacketDeliveryRatioText}."));
        NotifyCommands();
    }

    /// <summary>
    /// Будує свіжий ScenarioRuntime (кидає, якщо сценарій некоректний, до зміни стану),
    /// зупиняє таймер, підміняє сценарій і рушій та повертає стан «Готово».
    /// </summary>
    private void ReplaceRuntime(ScenarioDefinition scenario)
    {
        ScenarioRuntime runtime = ScenarioFactory.Build(scenario);

        _timer.Pause();
        _scenario = scenario;
        _engine = runtime.Simulation;
        ApplyRuntime(runtime);

        State = SimulationState.Ready;
        OnPropertyChanged(nameof(Scenario));
        NotifyCommands();
    }

    private void ApplyRuntime(ScenarioRuntime runtime)
    {
        _lastNodeByPacket.Clear();
        EventLog.Clear();
        Metrics.Update(_engine.GetSnapshot(), _engine.CurrentTimeMs);
        Map.SetTopology(runtime.Topology);
    }

    private void NotifyCommands()
    {
        PlayCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StepCommand.NotifyCanExecuteChanged();
    }
}