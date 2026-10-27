using MeshSim.Engine.IO;

namespace MeshSim.Desktop.Scenarios;

/// <summary>
/// Вбудований демонстраційний сценарій: шість вузлів із різним зарядом (один розряджений),
/// дві зони завад (активна й вимкнена) та один пакет N1 → N5.
/// </summary>
public static class DemoScenario
{
    public static ScenarioDefinition Create()
    {
        return new ScenarioDefinition
        {
            SchemaVersion = ScenarioDefinition.CurrentSchemaVersion,
            Name = "Демонстраційний сценарій",
            RoutingStrategy = RoutingStrategyRegistry.Dijkstra,
            Engine = new EngineSettingsDefinition { HopDelayMs = 20.0, EnergyPerTransmissionPercent = 1.5 },
            Nodes =
            [
                Node("N1", 0.0, 0.0, 100.0),
                Node("N2", 120.0, 60.0, 80.0),
                Node("N3", 240.0, 0.0, 65.0),
                Node("N4", 120.0, -70.0, 35.0),
                Node("N5", 360.0, 60.0, 12.0),
                Node("N6", 360.0, -70.0, 0.0)
            ],
            JammingZones =
            [
                new JammingZoneDefinition { Id = "J1", X = 180.0, Y = 40.0, RadiusMeters = 35.0, JammingIntensity = 0.6, IsActive = true },
                new JammingZoneDefinition { Id = "J2", X = 300.0, Y = -40.0, RadiusMeters = 30.0, JammingIntensity = 1.0, IsActive = false }
            ],
            Packets =
            [
                new PacketDefinition { Id = "P1", SourceNodeId = "N1", DestinationNodeId = "N5", Ttl = 8, PayloadBytes = 128, ScheduledTimeMs = 0.0 }
            ]
        };
    }

    private static NodeDefinition Node(string id, double x, double y, double battery)
    {
        return new NodeDefinition
        {
            Id = id,
            X = x,
            Y = y,
            TransmissionRangeMeters = 140.0,
            BatteryLevelPercent = battery
        };
    }
}