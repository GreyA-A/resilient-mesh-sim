using MeshSim.Desktop.Scenarios;
using MeshSim.Engine;
using MeshSim.Engine.IO;

namespace MeshSim.Tests;

public class DemoScenarioTests
{
    [Fact]
    public void DemoScenarioPassesValidation()
    {
        Assert.Empty(ScenarioValidator.Validate(DemoScenario.Create()));
    }

    [Fact]
    public void DemoScenarioShowsAllVisualStates()
    {
        ScenarioDefinition scenario = DemoScenario.Create();

        Assert.Contains(scenario.Nodes, node => node.BatteryLevelPercent == 0.0);
        Assert.Contains(scenario.Nodes, node => node.BatteryLevelPercent > 0.0 && node.BatteryLevelPercent < 20.0);
        Assert.Contains(scenario.JammingZones, zone => zone.IsActive);
        Assert.Contains(scenario.JammingZones, zone => !zone.IsActive);
    }

    [Fact]
    public void DemoPacketIsDeliveredAroundPartiallyJammedLink()
    {
        ScenarioRuntime runtime = ScenarioFactory.Build(DemoScenario.Create());

        SimulationResult result = runtime.Simulation.RunToCompletion();

        Assert.Equal(1, result.TotalPacketsDelivered);
        Assert.Equal(3.0, result.AverageHopCount);
    }
}