using MeshSim.Engine;
using MeshSim.Engine.IO;

namespace MeshSim.Tests;

/// <summary>
/// Перевіряє каталог scenarios/ у корені репозиторію: кожен файл має завантажуватись, будуватись і виконуватись,
/// а прикладні сценарії мають відтворювати задокументовані результати.
/// </summary>
public class ScenarioCatalogTests
{
    private static string FindScenariosDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "scenarios");

            if (File.Exists(Path.Combine(directory.FullName, "ResilientMesh.sln")) && Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Каталог scenarios/ у корені репозиторію (поруч із ResilientMesh.sln) не знайдено.");
    }

    public static TheoryData<string> CatalogFiles()
    {
        var data = new TheoryData<string>();

        foreach (string path in Directory.GetFiles(FindScenariosDirectory(), "*.json").OrderBy(file => file, StringComparer.Ordinal))
        {
            data.Add(Path.GetFileName(path));
        }

        return data;
    }

    private static SimulationResult RunScenario(string fileName)
    {
        ScenarioDefinition scenario = ScenarioFile.Load(Path.Combine(FindScenariosDirectory(), fileName));

        return ScenarioFactory.Build(scenario).Simulation.RunToCompletion();
    }

    [Fact]
    public void CatalogContainsTheExampleScenarios()
    {
        string[] present = Directory.GetFiles(FindScenariosDirectory(), "*.json")
            .Select(file => Path.GetFileName(file))
            .ToArray();

        Assert.Contains("jammed-reroute.json", present);
        Assert.Contains("battery-depletion.json", present);
        Assert.Contains("network-partition.json", present);
    }

    [Theory]
    [MemberData(nameof(CatalogFiles))]
    public void EveryCatalogScenarioLoadsBuildsAndRunsToCompletion(string fileName)
    {
        SimulationResult result = RunScenario(fileName);

        Assert.True(result.TotalPacketsSent > 0);
        Assert.Equal(result.TotalPacketsSent, result.TotalPacketsDelivered + result.TotalPacketsDropped);
    }

    [Fact]
    public void JammedRerouteDeliversBothPacketsAroundTheJammedLink()
    {
        SimulationResult result = RunScenario("jammed-reroute.json");

        Assert.Equal(2, result.TotalPacketsSent);
        Assert.Equal(2, result.TotalPacketsDelivered);
        Assert.Equal(0, result.TotalPacketsDropped);
        Assert.Equal(2.0, result.AverageHopCount);
    }

    [Fact]
    public void BatteryDepletionLosesOnePacketAndReroutesTheNext()
    {
        SimulationResult result = RunScenario("battery-depletion.json");

        Assert.Equal(3, result.TotalPacketsSent);
        Assert.Equal(2, result.TotalPacketsDelivered);
        Assert.Equal(1, result.TotalPacketsDropped);
        // Остання передача релея з нульовим залишком заряду програє: рушій фіксує причину JammedInTransit.
        Assert.Equal(1, result.DropsByReason[PacketDropReason.JammedInTransit]);
        Assert.Equal(2.0, result.AverageHopCount);
        Assert.Contains(result.EventLog, e => e.EventType == SimulationEventType.PacketForwarded && e.CurrentNodeId == "R2");
    }

    [Fact]
    public void NetworkPartitionDropsPacketWithNoRouteFound()
    {
        SimulationResult result = RunScenario("network-partition.json");

        Assert.Equal(3, result.TotalPacketsSent);
        Assert.Equal(2, result.TotalPacketsDelivered);
        Assert.Equal(1, result.TotalPacketsDropped);
        Assert.Equal(1, result.DropsByReason[PacketDropReason.NoRouteFound]);
        Assert.Equal(1.0, result.AverageHopCount);
    }
}