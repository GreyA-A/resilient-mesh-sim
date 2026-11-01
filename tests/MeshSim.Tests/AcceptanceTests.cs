using MeshSim.Desktop.Rendering;
using MeshSim.Desktop.ViewModels;
using MeshSim.Engine;

namespace MeshSim.Tests;

/// <summary>
/// Наскрізні приймальні тести (AC-01…AC-08, див. ACCEPTANCE.md): система перевіряється як єдине ціле
/// через MainViewModel (карта, симуляція, редактор, файлові операції), ручний таймер, фейкові діалоги
/// та реальні файли. Клас розбито на partial-файли за темами.
/// </summary>
public partial class AcceptanceTests
{
    // ================= AC-01: обхід активної зони РЕБ =================

    [Fact]
    public async Task Ac01JammedRerouteDeliversAllPacketsAroundTheJammedLink()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();

        await f.OpenCatalogScenarioAsync("jammed-reroute.json");

        Assert.Equal("Сценарій «Обхід зони РЕБ» відкрито з файлу jammed-reroute.json.", f.Main.StatusText);
        Assert.Equal("Dijkstra", f.Simulation.Scenario.RoutingStrategy);
        Assert.Equal(3, f.Map.Topology.Nodes.Count);
        Assert.Equal(3, f.Map.Scene.Links.Count);
        LinkVisual direct = f.Map.Scene.Links.First(link => link.FromId == "A" && link.ToId == "C");
        Assert.Equal(0.0, direct.Quality);
        Assert.True(direct.Style.IsDashed);
        Assert.Contains(f.Map.Scene.Zones, zone => zone.Id == "J1" && zone.Style.IsActive);
        Assert.Contains(f.Map.Scene.Zones, zone => zone.Id == "J2" && !zone.Style.IsActive);

        f.RunToEnd();

        Assert.Equal(2, f.Simulation.Metrics.TotalPacketsSent);
        Assert.Equal(2, f.Simulation.Metrics.TotalPacketsDelivered);
        Assert.Equal(0, f.Simulation.Metrics.TotalPacketsDropped);
        Assert.Equal("100.0 %", f.Simulation.Metrics.PacketDeliveryRatioText);
        Assert.Equal("2.00", f.Simulation.Metrics.AverageHopCountText);
        Assert.Empty(f.Simulation.Metrics.DropsByReason);

        SimulationResult result = f.Simulation.GetCurrentSnapshot();
        Assert.Contains(result.EventLog, e => e.EventType == SimulationEventType.PacketForwarded && e.Packet.Id == "P1" && e.CurrentNodeId == "B");
        Assert.Contains(result.EventLog, e => e.EventType == SimulationEventType.PacketForwarded && e.Packet.Id == "P2" && e.CurrentNodeId == "B");
        Assert.DoesNotContain(result.EventLog, e => e.EventType == SimulationEventType.PacketDropped);
    }

    // ================= AC-02: перебудова маршруту після виснаження ретранслятора =================

    [Fact]
    public async Task Ac02BatteryDepletionLosesOnePacketAndReroutesTheNext()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        await f.OpenCatalogScenarioAsync("battery-depletion.json");

        f.RunToEnd();

        Assert.Equal(3, f.Simulation.Metrics.TotalPacketsSent);
        Assert.Equal(2, f.Simulation.Metrics.TotalPacketsDelivered);
        Assert.Equal(1, f.Simulation.Metrics.TotalPacketsDropped);
        Assert.Equal("66.7 %", f.Simulation.Metrics.PacketDeliveryRatioText);
        Assert.Equal("2.00", f.Simulation.Metrics.AverageHopCountText);

        // Остання передача ретранслятора з нульовим залишком заряду програє: рушій фіксує причину JammedInTransit.
        DropReasonRow drop = Assert.Single(f.Simulation.Metrics.DropsByReason);
        Assert.Equal(PacketDropReason.JammedInTransit, drop.Reason);
        Assert.Equal(1, drop.Count);

        // R1: 30 → 5 (P1) → 0 (P2); R2 передав лише P3; A передав усі три пакети.
        Assert.Equal(0.0, f.Map.Topology.Nodes["R1"].BatteryLevelPercent);
        Assert.Equal(75.0, f.Map.Topology.Nodes["R2"].BatteryLevelPercent);
        Assert.Equal(25.0, f.Map.Topology.Nodes["A"].BatteryLevelPercent);

        SimulationResult result = f.Simulation.GetCurrentSnapshot();
        Assert.Contains(result.EventLog, e => e.EventType == SimulationEventType.PacketDropped && e.Packet.Id == "P2" && e.CurrentNodeId == "R1");
        Assert.Contains(result.EventLog, e => e.EventType == SimulationEventType.PacketForwarded && e.Packet.Id == "P3" && e.CurrentNodeId == "R2");

        // На карті розряджений вузол сірий і не має лінків.
        Assert.False(f.Map.Scene.Nodes.First(node => node.Id == "R1").Style.IsActive);
        Assert.All(f.Map.Scene.Links, link => Assert.True(link.FromId != "R1" && link.ToId != "R1"));
    }

    // ================= AC-03: розділення мережі завадою =================

    [Fact]
    public async Task Ac03JammingPartitionDropsPacketWithNoRouteFound()
    {
        using AcceptanceFixture f = AcceptanceFixture.Create();
        await f.OpenCatalogScenarioAsync("network-partition.json");

        Assert.Equal(3, f.Map.Scene.Links.Count);
        LinkVisual blocked = f.Map.Scene.Links.First(link => link.FromId == "B" && link.ToId == "C");
        Assert.Equal(0.0, blocked.Quality);
        Assert.True(blocked.Style.IsDashed);

        f.RunToEnd();

        Assert.Equal(3, f.Simulation.Metrics.TotalPacketsSent);
        Assert.Equal(2, f.Simulation.Metrics.TotalPacketsDelivered);
        Assert.Equal(1, f.Simulation.Metrics.TotalPacketsDropped);
        Assert.Equal("66.7 %", f.Simulation.Metrics.PacketDeliveryRatioText);
        Assert.Equal("1.00", f.Simulation.Metrics.AverageHopCountText);

        DropReasonRow drop = Assert.Single(f.Simulation.Metrics.DropsByReason);
        Assert.Equal(PacketDropReason.NoRouteFound, drop.Reason);
        Assert.Equal("немає маршруту", drop.Description);
        Assert.Equal(1, drop.Count);
        Assert.Contains("50.0 мс | P2 | Втрачено | вузол A | немає маршруту", f.Simulation.EventLog);
    }
}