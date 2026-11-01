using MeshSim.Desktop.ViewModels;

namespace MeshSim.Tests;

public partial class AcceptanceTests
{
    // ================= AC-04: Step, Pause, Reset і детермінований повторний прогін =================

    [Fact]
    public void Ac04StepPauseAndResetGiveDeterministicRepeatedRun()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        SimulationViewModel simulation = f.Simulation;

        // Дві події кроками (демонстраційний сценарій: пакет P1 N1 → N4 → N3 → N5).
        simulation.StepCommand.Execute(null);
        simulation.StepCommand.Execute(null);
        Assert.Equal(2, simulation.EventLog.Count);
        Assert.Equal(SimulationState.Paused, simulation.State);

        // Старт: один такт додає подію.
        simulation.PlayCommand.Execute(null);
        Assert.Equal(SimulationState.Running, simulation.State);
        Assert.True(f.Timer.IsRunning);
        f.Timer.FireTick();
        Assert.Equal(3, simulation.EventLog.Count);

        // Пауза: такти ігноруються.
        simulation.PauseCommand.Execute(null);
        Assert.False(f.Timer.IsRunning);
        f.Timer.FireTick();
        Assert.Equal(3, simulation.EventLog.Count);
        Assert.Equal(SimulationState.Paused, simulation.State);

        // Продовження до кінця: таймер зупиняється сам.
        simulation.PlayCommand.Execute(null);
        f.Timer.FireTick();
        Assert.Equal(SimulationState.Finished, simulation.State);
        Assert.False(f.Timer.IsRunning);
        Assert.Equal(4, simulation.EventLog.Count);
        Assert.Equal("100.0 %", simulation.Metrics.PacketDeliveryRatioText);
        Assert.Equal("3.00", simulation.Metrics.AverageHopCountText);
        Assert.Equal("60.0 мс", simulation.Metrics.CurrentTimeText);
        Assert.Equal(98.5, f.Map.Topology.Nodes["N1"].BatteryLevelPercent);
        string[] firstLog = simulation.EventLog.ToArray();

        // Скидання повертає початковий стан сценарію.
        simulation.ResetCommand.Execute(null);
        Assert.Equal(SimulationState.Ready, simulation.State);
        Assert.Empty(simulation.EventLog);
        Assert.Equal(1, simulation.Metrics.TotalPacketsSent);
        Assert.Equal(0, simulation.Metrics.TotalPacketsDelivered);
        Assert.Equal("0.0 мс", simulation.Metrics.CurrentTimeText);
        Assert.Null(f.Map.Activity);
        Assert.Equal(100.0, f.Map.Topology.Nodes["N1"].BatteryLevelPercent);
        Assert.True(simulation.StepCommand.CanExecute(null));

        // Повторний прогін дає ідентичний журнал і метрики.
        f.RunToEnd();
        Assert.Equal(firstLog, simulation.EventLog);
        Assert.Equal("100.0 %", simulation.Metrics.PacketDeliveryRatioText);
        Assert.Equal("60.0 мс", simulation.Metrics.CurrentTimeText);
        Assert.Equal(98.5, f.Map.Topology.Nodes["N1"].BatteryLevelPercent);
    }

    [Fact]
    public void Ac04SpeedSelectionChangesTickInterval()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        SimulationViewModel simulation = f.Simulation;
        Assert.Equal("1×", simulation.SelectedSpeed.Label);
        Assert.Equal(TimeSpan.FromMilliseconds(800), f.Timer.Interval);

        simulation.SelectedSpeed = simulation.SpeedOptions[4];
        Assert.Equal("4×", simulation.SelectedSpeed.Label);
        Assert.Equal(TimeSpan.FromMilliseconds(200), f.Timer.Interval);

        simulation.SelectedSpeed = simulation.SpeedOptions[0];
        Assert.Equal("0.25×", simulation.SelectedSpeed.Label);
        Assert.Equal(TimeSpan.FromMilliseconds(3200), f.Timer.Interval);
    }
}